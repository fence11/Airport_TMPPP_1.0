using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces
{

    /// Airport query service interface following Interface Segregation Principle (ISP)
    /// Defines only read operations for airports

    public interface IAirportQueryService
    {
        Task<IEnumerable<Airport>> GetAirportsAsync();
        Task<Airport?> GetAirportByIdAsync(int id);
        Task<Airport?> GetAirportByCodeAsync(string code);
        Task<bool> AirportExistsAsync(int id);
    }
}