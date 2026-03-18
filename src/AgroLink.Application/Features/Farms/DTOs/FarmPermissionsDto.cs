namespace AgroLink.Application.Features.Farms.DTOs;

public record FarmPermissionsDto
{
    // Animals
    public bool CanCreateAnimal { get; init; }
    public bool CanUpdateAnimalBio { get; init; }
    public bool CanDeleteAnimal { get; init; }

    // Operations
    public bool CanLogOperations { get; init; }
    public bool CanViewChecklists { get; init; }

    // Financials
    public bool CanViewFinancials { get; init; }
    public bool CanUpdateFinancials { get; init; }

    // Farm structure
    public bool CanManageLocations { get; init; }
    public bool CanManagePartners { get; init; }

    // Farm admin
    public bool CanUpdateFarmMetadata { get; init; }
    public bool CanManageTeam { get; init; }
    public bool CanDeleteFarm { get; init; }
}
