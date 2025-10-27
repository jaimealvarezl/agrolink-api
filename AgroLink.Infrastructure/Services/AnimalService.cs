using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Services;

public class AnimalService : IAnimalService
{
    private readonly IUnitOfWork _unitOfWork;

    public AnimalService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AnimalDto?> GetByIdAsync(int id)
    {
        var animal = await _unitOfWork.Animals.GetByIdAsync(id);
        if (animal == null) return null;

        return await MapToDtoAsync(animal);
    }

    public async Task<IEnumerable<AnimalDto>> GetAllAsync()
    {
        var animals = await _unitOfWork.Animals.GetAllAsync();
        var result = new List<AnimalDto>();

        foreach (var animal in animals)
        {
            result.Add(await MapToDtoAsync(animal));
        }

        return result;
    }

    public async Task<IEnumerable<AnimalDto>> GetByLotAsync(int lotId)
    {
        var animals = await _unitOfWork.Animals.FindAsync(a => a.LotId == lotId);
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

        await _unitOfWork.Animals.AddAsync(animal);
        await _unitOfWork.SaveChangesAsync();

        // Add owners
        foreach (var ownerDto in dto.Owners)
        {
            var animalOwner = new AnimalOwner
            {
                AnimalId = animal.Id,
                OwnerId = ownerDto.OwnerId,
                SharePercent = ownerDto.SharePercent
            };
            await _unitOfWork.AnimalOwners.AddAsync(animalOwner);
        }

        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(animal);
    }

    public async Task<AnimalDto> UpdateAsync(int id, UpdateAnimalDto dto)
    {
        var animal = await _unitOfWork.Animals.GetByIdAsync(id);
        if (animal == null) throw new ArgumentException("Animal not found");

        animal.Name = dto.Name ?? animal.Name;
        animal.Color = dto.Color ?? animal.Color;
        animal.Breed = dto.Breed ?? animal.Breed;
        animal.Status = dto.Status;
        animal.BirthDate = dto.BirthDate ?? animal.BirthDate;
        animal.MotherId = dto.MotherId ?? animal.MotherId;
        animal.FatherId = dto.FatherId ?? animal.FatherId;
        animal.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Animals.UpdateAsync(animal);

        // Update owners
        var existingOwners = await _unitOfWork.AnimalOwners.FindAsync(ao => ao.AnimalId == id);
        foreach (var existingOwner in existingOwners)
        {
            await _unitOfWork.AnimalOwners.DeleteAsync(existingOwner);
        }

        foreach (var ownerDto in dto.Owners)
        {
            var animalOwner = new AnimalOwner
            {
                AnimalId = id,
                OwnerId = ownerDto.OwnerId,
                SharePercent = ownerDto.SharePercent
            };
            await _unitOfWork.AnimalOwners.AddAsync(animalOwner);
        }

        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(animal);
    }

    public async Task DeleteAsync(int id)
    {
        var animal = await _unitOfWork.Animals.GetByIdAsync(id);
        if (animal == null) throw new ArgumentException("Animal not found");

        await _unitOfWork.Animals.DeleteAsync(animal);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<AnimalGenealogyDto?> GetGenealogyAsync(int id)
    {
        var animal = await _unitOfWork.Animals.GetByIdAsync(id);
        if (animal == null) return null;

        return await BuildGenealogyAsync(animal);
    }

    public async Task<AnimalDto> MoveAnimalAsync(int animalId, int fromLotId, int toLotId, string? reason, int userId)
    {
        var animal = await _unitOfWork.Animals.GetByIdAsync(animalId);
        if (animal == null) throw new ArgumentException("Animal not found");

        animal.LotId = toLotId;
        animal.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Animals.UpdateAsync(animal);

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

        await _unitOfWork.Movements.AddAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        return await MapToDtoAsync(animal);
    }

    private async Task<AnimalDto> MapToDtoAsync(Animal animal)
    {
        var lot = await _unitOfWork.Lots.GetByIdAsync(animal.LotId);
        var mother = animal.MotherId.HasValue ? await _unitOfWork.Animals.GetByIdAsync(animal.MotherId.Value) : null;
        var father = animal.FatherId.HasValue ? await _unitOfWork.Animals.GetByIdAsync(animal.FatherId.Value) : null;

        var owners = await _unitOfWork.AnimalOwners.FindAsync(ao => ao.AnimalId == animal.Id);
        var ownerDtos = new List<AnimalOwnerDto>();

        foreach (var owner in owners)
        {
            var ownerEntity = await _unitOfWork.Owners.GetByIdAsync(owner.OwnerId);
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

        var photos = await _unitOfWork.Photos.FindAsync(p => p.EntityType == "ANIMAL" && p.EntityId == animal.Id);
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
            var mother = await _unitOfWork.Animals.GetByIdAsync(animal.MotherId.Value);
            if (mother != null)
            {
                genealogy.Mother = await BuildGenealogyAsync(mother);
            }
        }

        if (animal.FatherId.HasValue)
        {
            var father = await _unitOfWork.Animals.GetByIdAsync(animal.FatherId.Value);
            if (father != null)
            {
                genealogy.Father = await BuildGenealogyAsync(father);
            }
        }

        // Get children
        var children = await _unitOfWork.Animals.FindAsync(a => a.MotherId == animal.Id || a.FatherId == animal.Id);
        foreach (var child in children)
        {
            genealogy.Children.Add(await BuildGenealogyAsync(child));
        }

        return genealogy;
    }
}