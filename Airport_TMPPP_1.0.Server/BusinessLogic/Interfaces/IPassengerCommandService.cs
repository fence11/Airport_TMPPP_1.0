using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces
{

    /// Passenger command service interface following Interface Segregation Principle (ISP)
    /// Defines only write operations for passengers

    public interface IPassengerCommandService
    {
        Task<Passenger> CreatePassengerAsync(string firstName, string lastName, string email, string? phoneNumber = null);
        Task<Passenger> UpdatePassengerAsync(int id, string? phoneNumber);
        Task<bool> DeletePassengerAsync(int id);
    }
}