using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces
{

    /// Flight query service interface following Interface Segregation Principle (ISP)
    /// Defines only read operations for flights

    public interface IFlightQueryService
    {
        Task<IEnumerable<Flight>> GetFlightsAsync();
        Task<Flight?> GetFlightByIdAsync(int id);
        Task<bool> FlightExistsAsync(int id);
    }
}