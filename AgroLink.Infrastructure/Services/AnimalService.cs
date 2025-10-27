using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Services;

public class AnimalService : IAnimalService
{
    private readonly AgroLinkDbContext _context;

    public AnimalService(AgroLinkDbContext context)
    {
        _context = context;
    }

    public async Task<AnimalDto?> GetByIdAsync(int id)
    {
        var animal = await _context.Animals.FindAsync(id);
        if (animal == null) return null;

        return await MapToDtoAsync(animal);
    }

    public async Task<IEnumerable<AnimalDto>> GetAllAsync()
    {
        var animals = await _context.Animals.ToListAsync();
        var result = new List<AnimalDto>();

        foreach (var animal in animals)
        {
            result.Add(await MapToDtoAsync(animal));
        }

        return result;
    }

    public async Task<IEnumerable<AnimalDto>> GetByLotAsync(int lotId)
    {
        var animals = await _context.Animals.Where(a => a.LotId == lotId).ToListAsync();
        var result = new List<AnimalDto>();

        foreach (var animal in animals)
        {
            result.Add(await MapToDtoAsync(animal));
        }

        return result;
    }

    public async Task<AnimalDto> CreateAsync(CreateAnimalDto dto)
    {
        var animal = new Animal
        {
            Tag = dto.Tag,
            Name = dto.Name,
            Color = dto.Color,
            Breed = dto.Breed,
            Sex = dto.Sex,
            BirthDate = dto.BirthDate,
            LotId = dto.LotId,
            MotherId = dto.MotherId,
            FatherId = dto.FatherId
        };

        _context.Animals.Add(animal);
        await _context.SaveChangesAsync();

        // Add owners
        foreach (var ownerDto in dto.Owners)
        {
            var animalOwner = new AnimalOwner
            {
                AnimalId = animal.Id,
                OwnerId = ownerDto.OwnerId,
                SharePercent = ownerDto.SharePercent
            };
            _context.AnimalOwners.Add(animalOwner);
        }

        await _context.SaveChangesAsync();
        return await MapToDtoAsync(animal);
    }

    public async Task<AnimalDto> UpdateAsync(int id, UpdateAnimalDto dto)
    {
        var animal = await _context.Animals.FindAsync(id);
        if (animal == null) throw new ArgumentException("Animal not found");

        animal.Name = dto.Name ?? animal.Name;
        animal.Color = dto.Color ?? animal.Color;
        animal.Breed = dto.Breed ?? animal.Breed;
        animal.Status = dto.Status;
        animal.BirthDate = dto.BirthDate ?? animal.BirthDate;
        animal.MotherId = dto.MotherId ?? animal.MotherId;
        animal.FatherId = dto.FatherId ?? animal.FatherId;
        animal.UpdatedAt = DateTime.UtcNow;

        _context.Animals.Update(animal);

        // Update owners
        var existingOwners = await _context.AnimalOwners.Where(ao => ao.AnimalId == id).ToListAsync();
        _context.AnimalOwners.RemoveRange(existingOwners);

        foreach (var ownerDto in dto.Owners)
        {
            var animalOwner = new AnimalOwner
            {
                AnimalId = id,
                OwnerId = ownerDto.OwnerId,
                SharePercent = ownerDto.SharePercent
            };
            _context.AnimalOwners.Add(animalOwner);
        }

        await _context.SaveChangesAsync();
        return await MapToDtoAsync(animal);
    }

    public async Task DeleteAsync(int id)
    {
        var animal = await _context.Animals.FindAsync(id);
        if (animal == null) throw new ArgumentException("Animal not found");

        _context.Animals.Remove(animal);
        await _context.SaveChangesAsync();
    }

    public async Task<AnimalGenealogyDto?> GetGenealogyAsync(int id)
    {
        var animal = await _context.Animals.FindAsync(id);
        if (animal == null) return null;

        return await BuildGenealogyAsync(animal);
    }

    public async Task<AnimalDto> MoveAnimalAsync(int animalId, int fromLotId, int toLotId, string? reason, int userId)
    {
        var animal = await _context.Animals.FindAsync(animalId);
        if (animal == null) throw new ArgumentException("Animal not found");

        animal.LotId = toLotId;
        animal.UpdatedAt = DateTime.UtcNow;

        _context.Animals.Update(animal);

        // Record movement
        var movement = new Movement
        {
            EntityType = "ANIMAL",
            EntityId = animalId,
            FromId = fromLotId,
            ToId = toLotId,
            At = DateTime.UtcNow,
            Reason = reason,
            UserId = userId
        };

        _context.Movements.Add(movement);
        await _context.SaveChangesAsync();

        return await MapToDtoAsync(animal);
    }

    private async Task<AnimalDto> MapToDtoAsync(Animal animal)
    {
        var lot = await _context.Lots.FindAsync(animal.LotId);
        var mother = animal.MotherId.HasValue ? await _context.Animals.FindAsync(animal.MotherId.Value) : null;
        var father = animal.FatherId.HasValue ? await _context.Animals.FindAsync(animal.FatherId.Value) : null;

        var owners = await _context.AnimalOwners.Where(ao => ao.AnimalId == animal.Id).ToListAsync();
        var ownerDtos = new List<AnimalOwnerDto>();

        foreach (var owner in owners)
        {
            var ownerEntity = await _context.Owners.FindAsync(owner.OwnerId);
            if (ownerEntity != null)
            {
                ownerDtos.Add(new AnimalOwnerDto
                {
                    OwnerId = owner.OwnerId,
                    OwnerName = ownerEntity.Name,
                    SharePercent = owner.SharePercent
                });
            }
        }

        var photos = await _context.Photos.Where(p => p.EntityType == "ANIMAL" && p.EntityId == animal.Id).ToListAsync();
        var photoDtos = photos.Select(p => new PhotoDto
        {
            Id = p.Id,
            EntityType = p.EntityType,
            EntityId = p.EntityId,
            UriLocal = p.UriLocal,
            UriRemote = p.UriRemote,
            Uploaded = p.Uploaded,
            Description = p.Description,
            CreatedAt = p.CreatedAt
        }).ToList();

        return new AnimalDto
        {
            Id = animal.Id,
            Tag = animal.Tag,
            Name = animal.Name,
            Color = animal.Color,
            Breed = animal.Breed,
            Sex = animal.Sex,
            Status = animal.Status,
            BirthDate = animal.BirthDate,
            LotId = animal.LotId,
            LotName = lot?.Name,
            MotherId = animal.MotherId,
            MotherTag = mother?.Tag,
            FatherId = animal.FatherId,
            FatherTag = father?.Tag,
            Owners = ownerDtos,
            Photos = photoDtos,
            CreatedAt = animal.CreatedAt,
            UpdatedAt = animal.UpdatedAt
        };
    }

    private async Task<AnimalGenealogyDto> BuildGenealogyAsync(Animal animal)
    {
        var genealogy = new AnimalGenealogyDto
        {
            Id = animal.Id,
            Tag = animal.Tag,
            Name = animal.Name,
            Sex = animal.Sex,
            BirthDate = animal.BirthDate
        };

        if (animal.MotherId.HasValue)
        {
            var mother = await _context.Animals.FindAsync(animal.MotherId.Value);
            if (mother != null)
            {
                genealogy.Mother = await BuildGenealogyAsync(mother);
            }
        }

        if (animal.FatherId.HasValue)
        {
            var father = await _context.Animals.FindAsync(animal.FatherId.Value);
            if (father != null)
            {
                genealogy.Father = await BuildGenealogyAsync(father);
            }
        }

        // Get children
        var children = await _context.Animals.Where(a => a.MotherId == animal.Id || a.FatherId == animal.Id).ToListAsync();
        foreach (var child in children)
        {
            genealogy.Children.Add(await BuildGenealogyAsync(child));
        }

        return genealogy;
    }
}