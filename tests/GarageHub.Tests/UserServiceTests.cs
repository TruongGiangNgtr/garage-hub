using System.Linq.Expressions;
using GarageHub.Common.Enums;
using GarageHub.Repository.Entities;
using GarageHub.Repository.Repositories;
using GarageHub.Service.Users;

namespace GarageHub.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task FindGoogleAccountAsync_MatchesEmailAndUpdatesGoogleProfile()
    {
        var existingUser = new User
        {
            Id = 12,
            FullName = "Previous Name",
            Email = "customer@example.com",
            GoogleId = null,
            Role = UserRole.Customer
        };
        var repository = new FakeUserRepository(existingUser);
        var unitOfWork = new FakeUnitOfWork();
        var service = new UserService(repository, unitOfWork);

        var result = await service.FindGoogleAccountAsync(
            "google-subject", "customer@example.com", "Current Name");

        Assert.NotNull(result);
        Assert.Equal(12, result.Id);
        Assert.Equal("customer@example.com", result.Email);
        Assert.Equal("Current Name", result.FullName);
        Assert.Equal("google-subject", existingUser.GoogleId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task FindGoogleAccountAsync_ReturnsNullWhenEmailAndGoogleIdAreUnknown()
    {
        var unitOfWork = new FakeUnitOfWork();
        var service = new UserService(new FakeUserRepository(), unitOfWork);

        var result = await service.FindGoogleAccountAsync(
            "google-subject", "new@example.com", "New Customer");

        Assert.Null(result);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private sealed class FakeUserRepository(params User[] users) : IRepository<User>
    {
        private readonly List<User> _users = [.. users];

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<User>>(_users);

        public Task<IReadOnlyList<User>> FindAsync(
            Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<User>>(_users.Where(predicate.Compile()).ToArray());

        public Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            _users.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(User entity) { }

        public void Remove(User entity) => _users.Remove(entity);
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
