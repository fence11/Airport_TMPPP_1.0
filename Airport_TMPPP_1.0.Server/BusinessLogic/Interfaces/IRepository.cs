using Airport_TMPPP_1._0.Server.BusinessLogic.Common;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces
{

    /// Generic repository interface following Interface Segregation Principle (ISP)
    /// and Dependency Inversion Principle (DIP)
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>

    public interface IRepository<T> where T : IEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
