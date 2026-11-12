namespace GarageHub.Repository.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly GarageHubDbContext _context;

    public UnitOfWork(GarageHubDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
