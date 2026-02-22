using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces
{

    /// Passenger query service interface following Interface Segregation Principle (ISP)
    /// Defines only read operations for passengers

    public interface IPassengerQueryService
    {
        Task<IEnumerable<Passenger>> GetPassengersAsync();
        Task<Passenger?> GetPassengerByIdAsync(int id);
        Task<bool> PassengerExistsAsync(int id);
    }
}