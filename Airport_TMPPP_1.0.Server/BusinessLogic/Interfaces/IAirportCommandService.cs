using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces
{

    /// Airport command service interface following Interface Segregation Principle (ISP)
    /// Defines only write operations for airports

    public interface IAirportCommandService
    {
        Task<Airport> CreateAirportAsync(string name, string code);
        Task<Airport> UpdateAirportAsync(int id, string name, string code);
        Task<bool> DeleteAirportAsync(int id);
    }
}