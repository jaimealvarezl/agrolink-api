namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalNoteDto
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateAnimalNoteDto
{
    public string Content { get; set; } = string.Empty;
}
