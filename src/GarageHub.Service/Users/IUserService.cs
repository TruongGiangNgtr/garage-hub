using GarageHub.Common.Dtos.Account;

namespace GarageHub.Service.Users;

public interface IUserService
{
    Task<UserSessionDto?> FindGoogleAccountAsync(
        string googleId, string email, string fullName, CancellationToken cancellationToken = default);
    Task<UserSessionDto> CreateGoogleCustomerAsync(
        string googleId, string email, string fullName, CancellationToken cancellationToken = default);
}
