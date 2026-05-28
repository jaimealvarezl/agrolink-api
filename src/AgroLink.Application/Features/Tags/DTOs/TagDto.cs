namespace AgroLink.Application.Features.Tags.DTOs;

public class TagDto
{
    public required int Id { get; set; }
    public required string DisplayName { get; set; } = string.Empty;
    public required int UsageCount { get; set; }
    public string? ColorToken { get; set; }
}
