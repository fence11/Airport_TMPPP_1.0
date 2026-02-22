using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces
{

    /// Unit of Work interface following Interface Segregation Principle (ISP)
    /// Provides transaction management and repository access

    public interface IUnitOfWork : IDisposable
    {
        IRepository<Airport> Airports { get; }
        IRepository<Flight> Flights { get; }
        IRepository<Passenger> Passengers { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
