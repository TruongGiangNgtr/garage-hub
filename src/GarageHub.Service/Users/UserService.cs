using GarageHub.Common.Dtos.User;
using GarageHub.Common.Enums;
using GarageHub.Common.Exceptions;
using GarageHub.Repository.Entities;
using GarageHub.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GarageHub.Service.Users;

public class UserService : IUserService
{
    private const string AdminEmailsKey = "Auth:AdminEmails";

    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HashSet<string> _adminEmails;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        // Set with dotnet user-secrets as a JSON array: "Auth": { "AdminEmails": [ "a@gmail.com" ] }
        _adminEmails = configuration.GetSection(AdminEmailsKey).GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => NormalizeEmail(v!))
            .ToHashSet();
    }

    public async Task<UserDto> SignInWithGoogleAsync(GoogleUserInfo info, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(info.GoogleId) || string.IsNullOrWhiteSpace(info.Email))
        {
            throw new ValidationException("Tài khoản Google không cung cấp đủ thông tin (ID hoặc email)");
        }

        var googleId = info.GoogleId.Trim();
        var email = NormalizeEmail(info.Email);
        var fullName = string.IsNullOrWhiteSpace(info.FullName) ? email : info.FullName.Trim();

        var user = await _userRepository.GetByGoogleIdAsync(googleId, cancellationToken);
        if (user is null)
        {
            user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user is not null)
            {
                if (user.GoogleId is not null)
                {
                    throw new ConflictException("Email này đã gắn với một tài khoản Google khác");
                }
                // Row created before the first login (e.g. added by hand): link it to this Google account.
                user.GoogleId = googleId;
            }
            else
            {
                // CreatedAt is filled by the DB default (Vietnam time).
                user = new User { GoogleId = googleId, Email = email, FullName = fullName, Role = UserRole.Customer };
                await _userRepository.AddAsync(user, cancellationToken);
            }
        }

        if (_adminEmails.Contains(NormalizeEmail(user.Email)))
        {
            user.Role = UserRole.Admin;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique index on GoogleId/Email: the same account is signing in twice at the same moment.
            throw new ConflictException("Đăng nhập đang được xử lý, vui lòng thử lại");
        }

        return ToDto(user);
    }

    public async Task<UserDto?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public Task<UserRole?> GetRoleAsync(int userId, CancellationToken cancellationToken = default)
        => _userRepository.GetRoleAsync(userId, cancellationToken);

    public async Task<UserPageDto> GetUsersAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var (items, totalCount) = await _userRepository.GetPagedAsync(search, page, pageSize, cancellationToken);
        return new UserPageDto(items.Select(ToDto).ToList(), totalCount, page, pageSize, search);
    }

    public async Task ChangeRoleAsync(int actingUserId, int userId, UserRole role, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ValidationException("Role không hợp lệ");
        }
        if (actingUserId == userId)
        {
            throw new ConflictException("Không thể tự đổi role của chính mình");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy người dùng");

        if (_adminEmails.Contains(NormalizeEmail(user.Email)))
        {
            throw new ConflictException("Tài khoản này là Admin theo cấu hình (Auth:AdminEmails), không đổi role được");
        }
        if (user.Role == role) return;

        user.Role = role;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static UserDto ToDto(User user) => new(user.Id, user.FullName, user.Email, user.Role, user.CreatedAt);
}
