using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IDeviceTokenRepository
{
    Task UpsertAsync(DeviceToken token, CancellationToken ct);
    Task<IReadOnlyList<string>> GetTokensByFarmAsync(int farmId, CancellationToken ct);
    Task DeleteAsync(string token, int userId, CancellationToken ct);
}
