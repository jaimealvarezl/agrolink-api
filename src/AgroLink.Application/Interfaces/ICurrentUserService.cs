namespace AgroLink.Application.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    int? CurrentFarmId { get; }
    string? CurrentFarmRole { get; }
    int GetRequiredUserId();
}
