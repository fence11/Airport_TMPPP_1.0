using Airport_TMPPP_1._0.Server.BusinessLogic.Common;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;
using Airport_TMPPP_1._0.Server.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Airport_TMPPP_1._0.Server.Data.Repositories
{

    /// Generic repository implementation following:
    /// - Single Responsibility Principle (SRP): Handles only data access operations
    /// - Dependency Inversion Principle (DIP): Depends on IDbContext abstraction
    /// - Open/Closed Principle (OCP): Can be extended without modification
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>

    public class Repository<T> : IRepository<T> where T : class, IEntity
    {
        protected readonly IDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(IDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.AnyAsync(e => e.Id == id);
        }
    }
}

