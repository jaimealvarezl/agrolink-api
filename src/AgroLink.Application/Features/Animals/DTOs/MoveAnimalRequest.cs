namespace AgroLink.Application.Features.Animals.DTOs;

public class MoveAnimalRequest
{
    public int FromLotId { get; set; }
    public int ToLotId { get; set; }
    public string? Reason { get; set; }
}
