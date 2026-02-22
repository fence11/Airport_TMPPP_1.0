using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;
using Airport_TMPPP_1._0.Server.Data.Context;
using Airport_TMPPP_1._0.Server.Data.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Airport_TMPPP_1._0.Server.Data.Repositories
{

    /// Unit of Work implementation following:
    /// - Single Responsibility Principle (SRP): Manages transactions and coordinates repositories
    /// - Dependency Inversion Principle (DIP): Depends on IDbContext and IRepository abstractions
    /// - Open/Closed Principle (OCP): Can be extended with new repositories without modification

    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbContext _context;
        private IDbContextTransaction? _transaction;
        private IRepository<Airport>? _airports;
        private IRepository<Flight>? _flights;
        private IRepository<Passenger>? _passengers;

        public UnitOfWork(IDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IRepository<Airport> Airports
        {
            get
            {
                return _airports ??= new Repository<Airport>(_context);
            }
        }

        public IRepository<Flight> Flights
        {
            get
            {
                return _flights ??= new Repository<Flight>(_context);
            }
        }

        public IRepository<Passenger> Passengers
        {
            get
            {
                return _passengers ??= new Repository<Passenger>(_context);
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            if (_context is AirportDbContext dbContext)
            {
                _transaction = await dbContext.Database.BeginTransactionAsync();
            }
            else
            {
                throw new NotSupportedException("Transaction support requires AirportDbContext implementation");
            }
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit");
            }

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to rollback");
            }

            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}
