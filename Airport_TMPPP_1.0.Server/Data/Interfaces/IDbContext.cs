using Microsoft.EntityFrameworkCore;

namespace Airport_TMPPP_1._0.Server.Data.Interfaces
{

    /// Database context interface following Dependency Inversion Principle (DIP)
    /// Allows abstraction of database operations for better testability and flexibility

    public interface IDbContext
    {
        DbSet<T> Set<T>() where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        int SaveChanges();
    }
}
