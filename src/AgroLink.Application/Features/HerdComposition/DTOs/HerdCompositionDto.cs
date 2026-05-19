namespace AgroLink.Application.Features.HerdComposition.DTOs;

public class HerdCompositionDto
{
    public List<OwnerGroupDto> ByOwnerGroup { get; set; } = [];
    public List<LotCountDto> ByLot { get; set; } = [];
    public List<LotSexCountDto> ByLotAndSex { get; set; } = [];
}

public class OwnerGroupDto
{
    public List<string> OwnerNames { get; set; } = [];
    public int AnimalCount { get; set; }
}

public class LotCountDto
{
    public int LotId { get; set; }
    public string LotName { get; set; } = string.Empty;
    public int AnimalCount { get; set; }
}

public class LotSexCountDto
{
    public int LotId { get; set; }
    public string LotName { get; set; } = string.Empty;
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
}
