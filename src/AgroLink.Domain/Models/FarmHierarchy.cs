namespace AgroLink.Domain.Models;

public class FarmHierarchy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PaddockHierarchy> Paddocks { get; set; } = new();
}

public class PaddockHierarchy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<LotHierarchy> Lots { get; set; } = new();
}

public class LotHierarchy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AnimalCount { get; set; }
}
