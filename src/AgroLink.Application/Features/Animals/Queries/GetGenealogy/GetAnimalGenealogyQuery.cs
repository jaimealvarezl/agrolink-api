using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetGenealogy;

public record GetAnimalGenealogyQuery(int Id) : IRequest<AnimalGenealogyDto?>;

public class GetAnimalGenealogyQueryHandler(IAnimalRepository animalRepository)
    : IRequestHandler<GetAnimalGenealogyQuery, AnimalGenealogyDto?>
{
    public async Task<AnimalGenealogyDto?> Handle(
        GetAnimalGenealogyQuery request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetAnimalWithGenealogyAsync(request.Id);
        if (animal == null)
        {
            return null;
        }

        return await BuildGenealogyAsync(animal);
    }

    private async Task<AnimalGenealogyDto> BuildGenealogyAsync(Animal animal)
    {
        var genealogy = new AnimalGenealogyDto
        {
            Id = animal.Id,
            Cuia = animal.Cuia,
            TagVisual = animal.TagVisual,
            Name = animal.Name,
            Sex = animal.Sex,
            BirthDate = animal.BirthDate,
            Children = [],
        };

        if (animal.MotherId.HasValue)
        {
            var mother = await animalRepository.GetByIdAsync(animal.MotherId.Value);
            if (mother != null)
            {
                genealogy.Mother = await BuildGenealogyAsync(mother);
            }
        }

        if (animal.FatherId.HasValue)
        {
            var father = await animalRepository.GetByIdAsync(animal.FatherId.Value);
            if (father != null)
            {
                genealogy.Father = await BuildGenealogyAsync(father);
            }
        }

        // Get children
        var children = await animalRepository.GetChildrenAsync(animal.Id);
        foreach (var child in children)
        {
            genealogy.Children.Add(await BuildGenealogyAsync(child));
        }

        return genealogy;
    }
}
