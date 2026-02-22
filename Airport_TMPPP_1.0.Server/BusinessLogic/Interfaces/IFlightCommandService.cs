using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces
{

    /// Flight command service interface following Interface Segregation Principle (ISP)
    /// Defines only write operations for flights

    public interface IFlightCommandService
    {
        Task<Flight> CreateFlightAsync(string flightNumber, int airportId);
        Task<Flight> UpdateFlightAsync(int id, string flightNumber, int airportId);
        Task<bool> DeleteFlightAsync(int id);
    }
}