using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class CreateBcsReadingDto
{
    public double Score { get; init; }
    public BcsReadingSource Source { get; init; }
    public bool HasAlerts { get; init; }
    public string? AlertDescription { get; init; }
    public string? RawAiResponse { get; init; }
}
