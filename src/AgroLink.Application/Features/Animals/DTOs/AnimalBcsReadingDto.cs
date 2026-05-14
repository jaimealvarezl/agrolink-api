using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalBcsReadingDto
{
    public int Id { get; init; }
    public int AnimalId { get; init; }
    public double Score { get; init; }
    public BcsReadingSource Source { get; init; }
    public int ConfirmedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
