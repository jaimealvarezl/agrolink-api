namespace AgroLink.Api.DTOs.Paddocks;

public class UpdatePaddockRequest
{
    public string? Name { get; set; }
    public int? FarmId { get; set; }
    public decimal? Area { get; set; }
    public string? AreaType { get; set; }
}
