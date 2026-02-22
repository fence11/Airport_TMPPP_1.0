using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Services
{

    /// Flight service implementation following:
    /// - Single Responsibility Principle (SRP): Handles only flight business logic
    /// - Dependency Inversion Principle (DIP): Depends on IUnitOfWork abstraction
    /// - Open/Closed Principle (OCP): Can be extended without modification
    /// - Interface Segregation Principle (ISP): Implements separate query and command interfaces

    public class FlightService : IFlightQueryService, IFlightCommandService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FlightService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IEnumerable<Flight>> GetFlightsAsync()
        {
            return await _unitOfWork.Flights.GetAllAsync();
        }

        public async Task<Flight?> GetFlightByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Flight ID must be greater than zero", nameof(id));

            return await _unitOfWork.Flights.GetByIdAsync(id);
        }

        public async Task<Flight> CreateFlightAsync(string flightNumber, int airportId)
        {
            if (string.IsNullOrWhiteSpace(flightNumber))
                throw new ArgumentException("Flight number cannot be empty", nameof(flightNumber));

            if (airportId <= 0)
                throw new ArgumentException("Airport ID must be greater than zero", nameof(airportId));

            // Verify airport exists
            var airport = await _unitOfWork.Airports.GetByIdAsync(airportId);
            if (airport == null)
                throw new InvalidOperationException($"Airport with ID {airportId} does not exist");

            var flight = new Flight(flightNumber, airportId);
            return await _unitOfWork.Flights.AddAsync(flight);
        }

        public async Task<Flight> UpdateFlightAsync(int id, string flightNumber, int airportId)
        {
            if (id <= 0)
                throw new ArgumentException("Flight ID must be greater than zero", nameof(id));

            if (string.IsNullOrWhiteSpace(flightNumber))
                throw new ArgumentException("Flight number cannot be empty", nameof(flightNumber));

            if (airportId <= 0)
                throw new ArgumentException("Airport ID must be greater than zero", nameof(airportId));

            var flight = await _unitOfWork.Flights.GetByIdAsync(id);
            if (flight == null)
                throw new InvalidOperationException($"Flight with ID {id} does not exist");

            // Verify airport exists
            var airport = await _unitOfWork.Airports.GetByIdAsync(airportId);
            if (airport == null)
                throw new InvalidOperationException($"Airport with ID {airportId} does not exist");

            var updatedFlight = new Flight(flightNumber, airportId);
            updatedFlight.GetType().GetProperty("Id")?.SetValue(updatedFlight, id);
            
            return await _unitOfWork.Flights.UpdateAsync(updatedFlight);
        }

        public async Task<bool> DeleteFlightAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Flight ID must be greater than zero", nameof(id));

            return await _unitOfWork.Flights.DeleteAsync(id);
        }

        public async Task<bool> FlightExistsAsync(int id)
        {
            if (id <= 0)
                return false;

            return await _unitOfWork.Flights.ExistsAsync(id);
        }
    }
}
