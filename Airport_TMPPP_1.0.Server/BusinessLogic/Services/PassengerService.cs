using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Services
{

    /// Passenger service implementation following:
    /// - Single Responsibility Principle (SRP): Handles only passenger business logic
    /// - Dependency Inversion Principle (DIP): Depends on IUnitOfWork abstraction
    /// - Open/Closed Principle (OCP): Can be extended without modification
    /// - Interface Segregation Principle (ISP): Implements separate query and command interfaces

    public class PassengerService : IPassengerQueryService, IPassengerCommandService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PassengerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IEnumerable<Passenger>> GetPassengersAsync()
        {
            return await _unitOfWork.Passengers.GetAllAsync();
        }

        public async Task<Passenger?> GetPassengerByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Passenger ID must be greater than zero", nameof(id));

            return await _unitOfWork.Passengers.GetByIdAsync(id);
        }

        public async Task<Passenger> CreatePassengerAsync(string firstName, string lastName, string email, string? phoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be empty", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be empty", nameof(lastName));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            var passenger = new Passenger(firstName, lastName, email, phoneNumber);
            return await _unitOfWork.Passengers.AddAsync(passenger);
        }

        public async Task<Passenger> UpdatePassengerAsync(int id, string? phoneNumber)
        {
            if (id <= 0)
                throw new ArgumentException("Passenger ID must be greater than zero", nameof(id));

            var passenger = await _unitOfWork.Passengers.GetByIdAsync(id);
            if (passenger == null)
                throw new InvalidOperationException($"Passenger with ID {id} does not exist");

            passenger.UpdateContactInfo(phoneNumber);
            return await _unitOfWork.Passengers.UpdateAsync(passenger);
        }

        public async Task<bool> DeletePassengerAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Passenger ID must be greater than zero", nameof(id));

            return await _unitOfWork.Passengers.DeleteAsync(id);
        }

        public async Task<bool> PassengerExistsAsync(int id)
        {
            if (id <= 0)
                return false;

            return await _unitOfWork.Passengers.ExistsAsync(id);
        }
    }
}
