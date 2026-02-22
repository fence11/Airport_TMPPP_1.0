using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Services
{

    /// Airport service implementation following:
    /// - Single Responsibility Principle (SRP): Handles only airport business logic
    /// - Dependency Inversion Principle (DIP): Depends on IUnitOfWork abstraction
    /// - Open/Closed Principle (OCP): Can be extended without modification
    /// - Interface Segregation Principle (ISP): Implements separate query and command interfaces

    public class AirportService : IAirportQueryService, IAirportCommandService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AirportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IEnumerable<Airport>> GetAirportsAsync()
        {
            return await _unitOfWork.Airports.GetAllAsync();
        }

        public async Task<Airport?> GetAirportByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Airport ID must be greater than zero", nameof(id));

            return await _unitOfWork.Airports.GetByIdAsync(id);
        }

        public async Task<Airport?> GetAirportByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Airport code cannot be empty", nameof(code));

            var airports = await _unitOfWork.Airports.GetAllAsync();
            return airports.FirstOrDefault(a => a.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Airport> CreateAirportAsync(string name, string code)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Airport name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Airport code cannot be empty", nameof(code));

            // Check if airport with same code already exists
            var existingAirport = await GetAirportByCodeAsync(code);
            if (existingAirport != null)
                throw new InvalidOperationException($"Airport with code {code} already exists");

            var airport = new Airport(name, code);
            return await _unitOfWork.Airports.AddAsync(airport);
        }

        public async Task<Airport> UpdateAirportAsync(int id, string name, string code)
        {
            if (id <= 0)
                throw new ArgumentException("Airport ID must be greater than zero", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Airport name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Airport code cannot be empty", nameof(code));

            var airport = await _unitOfWork.Airports.GetByIdAsync(id);
            if (airport == null)
                throw new InvalidOperationException($"Airport with ID {id} does not exist");

            // Check if another airport with same code exists
            var existingAirport = await GetAirportByCodeAsync(code);
            if (existingAirport != null && existingAirport.Id != id)
                throw new InvalidOperationException($"Airport with code {code} already exists");

            var updatedAirport = new Airport(name, code);
            updatedAirport.GetType().GetProperty("Id")?.SetValue(updatedAirport, id);
            
            return await _unitOfWork.Airports.UpdateAsync(updatedAirport);
        }

        public async Task<bool> DeleteAirportAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Airport ID must be greater than zero", nameof(id));

            // Check if airport has associated flights
            var flights = await _unitOfWork.Flights.GetAllAsync();
            if (flights.Any(f => f.AirportId == id))
                throw new InvalidOperationException($"Cannot delete airport with ID {id} because it has associated flights");

            return await _unitOfWork.Airports.DeleteAsync(id);
        }

        public async Task<bool> AirportExistsAsync(int id)
        {
            if (id <= 0)
                return false;

            return await _unitOfWork.Airports.ExistsAsync(id);
        }
    }
}

