using GarageHub.Common.Dtos.Account;
using GarageHub.Common.Enums;
using GarageHub.Common.Exceptions;
using GarageHub.Repository.Entities;
using GarageHub.Repository.Repositories;

namespace GarageHub.Service.Users;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IRepository<User> userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSessionDto?> FindGoogleAccountAsync(
        string googleId, string email, string fullName, CancellationToken cancellationToken = default)
    {
        googleId = googleId.Trim();
        email = email.Trim();
        fullName = fullName.Trim();

        var googleUsers = await _userRepository.FindAsync(user => user.GoogleId == googleId, cancellationToken);
        var emailUsers = await _userRepository.FindAsync(user => user.Email == email, cancellationToken);
        var googleUser = googleUsers.FirstOrDefault();
        var emailUser = emailUsers.FirstOrDefault();
        if (googleUser is not null && emailUser is not null && googleUser.Id != emailUser.Id)
        {
            throw new ConflictException("This Google account conflicts with an existing account.");
        }

        var user = emailUser ?? googleUser;
        if (user is null)
        {
            return null;
        }

        var changed = false;
        if (user.GoogleId != googleId)
        {
            user.GoogleId = googleId;
            changed = true;
        }
        if (user.Email != email)
        {
            user.Email = email;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(fullName) && user.FullName != fullName)
        {
            user.FullName = fullName;
            changed = true;
        }
        if (changed)
        {
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToSession(user);
    }

    public async Task<UserSessionDto> CreateGoogleCustomerAsync(
        string googleId, string email, string fullName, CancellationToken cancellationToken = default)
    {
        googleId = googleId.Trim();
        email = email.Trim();
        fullName = fullName.Trim();
        if (string.IsNullOrWhiteSpace(googleId)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(fullName))
        {
            throw new ValidationException("Google ID, email, and full name are required.");
        }

        var existingGoogleUser = await _userRepository.FindAsync(
            user => user.GoogleId == googleId, cancellationToken);
        if (existingGoogleUser.Count > 0)
        {
            return ToSession(existingGoogleUser[0]);
        }

        var existingEmail = await _userRepository.FindAsync(
            user => user.Email == email, cancellationToken);
        if (existingEmail.Count > 0)
        {
            throw new ConflictException("An account already uses this email. Sign in with its linked Google account.");
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            GoogleId = googleId,
            Role = UserRole.Customer
        };
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToSession(user);
    }

    private static UserSessionDto ToSession(User user)
        => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
}
