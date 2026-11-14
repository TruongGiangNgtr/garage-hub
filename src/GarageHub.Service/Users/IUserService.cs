using GarageHub.Common.Dtos.User;
using GarageHub.Common.Enums;

namespace GarageHub.Service.Users;

public interface IUserService
{
    /// <summary>
    /// Maps a Google login to a Users row: find by GoogleId, else link an existing row with the same email
    /// and no GoogleId, else create a Customer. Emails listed in Auth:AdminEmails are promoted to Admin.
    /// Throws ValidationException for missing Google data, ConflictException when the email belongs to another Google account.
    /// </summary>
    Task<UserDto> SignInWithGoogleAsync(GoogleUserInfo info, CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Current role from the database (used to refresh the auth cookie). Null when the user no longer exists.</summary>
    Task<UserRole?> GetRoleAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserPageDto> GetUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin changes another user's role. Throws NotFoundException for an unknown user, ConflictException when
    /// changing one's own role or the role of an email listed in Auth:AdminEmails.
    /// </summary>
    Task ChangeRoleAsync(int actingUserId, int userId, UserRole role, CancellationToken cancellationToken = default);
}
