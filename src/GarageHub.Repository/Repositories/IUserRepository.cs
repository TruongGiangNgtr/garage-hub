using GarageHub.Common.Enums;
using GarageHub.Repository.Entities;

namespace GarageHub.Repository.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Returns the role only (no tracking). Null when the user no longer exists.</summary>
    Task<UserRole?> GetRoleAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Filters by email or full name (contains). Newest users first.</summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
