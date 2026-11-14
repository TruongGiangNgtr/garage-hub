using GarageHub.Common.Dtos.User;
using GarageHub.Common.Enums;
using GarageHub.Common.Exceptions;
using GarageHub.Repository.Entities;
using GarageHub.Repository.Repositories;
using GarageHub.Service.Users;
using Microsoft.Extensions.Configuration;

namespace GarageHub.Tests.Users;

public class UserServiceTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private UserService CreateService(params string[] adminEmails)
    {
        var settings = adminEmails
            .Select((email, i) => new KeyValuePair<string, string?>($"Auth:AdminEmails:{i}", email));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new UserService(_repository, _unitOfWork, configuration);
    }

    [Fact]
    public async Task SignIn_NewGoogleAccount_CreatesCustomer()
    {
        var service = CreateService();

        var user = await service.SignInWithGoogleAsync(new GoogleUserInfo("g-1", " An@Gmail.com ", "Nguyen An"));

        var stored = Assert.Single(_repository.Users);
        Assert.Equal("g-1", stored.GoogleId);
        Assert.Equal("an@gmail.com", stored.Email);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task SignIn_ExistingGoogleId_ReturnsSameUserWithoutCreating()
    {
        _repository.Users.Add(new User { Id = 5, GoogleId = "g-1", Email = "an@gmail.com", FullName = "An", Role = UserRole.GarageOwner });
        var service = CreateService();

        var user = await service.SignInWithGoogleAsync(new GoogleUserInfo("g-1", "an@gmail.com", "An"));

        Assert.Equal(5, user.Id);
        Assert.Equal(UserRole.GarageOwner, user.Role);
        Assert.Single(_repository.Users);
    }

    [Fact]
    public async Task SignIn_EmailExistsWithoutGoogleId_LinksGoogleId()
    {
        _repository.Users.Add(new User { Id = 7, GoogleId = null, Email = "an@gmail.com", FullName = "An", Role = UserRole.Customer });
        var service = CreateService();

        var user = await service.SignInWithGoogleAsync(new GoogleUserInfo("g-9", "AN@gmail.com", "An"));

        Assert.Equal(7, user.Id);
        Assert.Equal("g-9", _repository.Users.Single().GoogleId);
    }

    [Fact]
    public async Task SignIn_EmailLinkedToOtherGoogleAccount_Throws()
    {
        _repository.Users.Add(new User { Id = 7, GoogleId = "g-other", Email = "an@gmail.com", FullName = "An" });
        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(
            () => service.SignInWithGoogleAsync(new GoogleUserInfo("g-9", "an@gmail.com", "An")));
    }

    [Fact]
    public async Task SignIn_MissingEmail_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.SignInWithGoogleAsync(new GoogleUserInfo("g-1", "", "An")));
    }

    [Fact]
    public async Task SignIn_ConfiguredAdminEmail_PromotesToAdmin()
    {
        _repository.Users.Add(new User { Id = 3, GoogleId = "g-1", Email = "boss@gmail.com", FullName = "Boss", Role = UserRole.Customer });
        var service = CreateService("Boss@Gmail.com");

        var user = await service.SignInWithGoogleAsync(new GoogleUserInfo("g-1", "boss@gmail.com", "Boss"));

        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task ChangeRole_OtherUser_UpdatesRole()
    {
        _repository.Users.Add(new User { Id = 2, Email = "a@gmail.com", Role = UserRole.Customer });
        var service = CreateService();

        await service.ChangeRoleAsync(actingUserId: 1, userId: 2, UserRole.GarageOwner);

        Assert.Equal(UserRole.GarageOwner, _repository.Users.Single().Role);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ChangeRole_Self_Throws()
    {
        _repository.Users.Add(new User { Id = 1, Email = "a@gmail.com", Role = UserRole.Admin });
        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() => service.ChangeRoleAsync(1, 1, UserRole.Customer));
    }

    [Fact]
    public async Task ChangeRole_ConfiguredAdmin_Throws()
    {
        _repository.Users.Add(new User { Id = 2, Email = "boss@gmail.com", Role = UserRole.Admin });
        var service = CreateService("boss@gmail.com");

        await Assert.ThrowsAsync<ConflictException>(() => service.ChangeRoleAsync(1, 2, UserRole.Customer));
    }

    [Fact]
    public async Task ChangeRole_UnknownUser_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.ChangeRoleAsync(1, 99, UserRole.Customer));
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.FirstOrDefault(u => u.Id == userId));

        public Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.FirstOrDefault(u => u.GoogleId == googleId));

        // SQL Server's default collation is case-insensitive.
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<UserRole?> GetRoleAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.FirstOrDefault(u => u.Id == userId)?.Role);

        public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
            string? search, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<User>, int)>((Users, Users.Count));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            user.Id = Users.Count == 0 ? 1 : Users.Max(u => u.Id) + 1;
            Users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
