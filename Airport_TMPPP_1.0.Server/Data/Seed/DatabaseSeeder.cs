using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.Data.Seed
{

    /// Database seeder following Single Responsibility Principle (SRP)
    /// Responsible only for seeding initial data into the database

    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IUnitOfWork unitOfWork)
        {
            // Check if data already exists
            var existingAirports = await unitOfWork.Airports.GetAllAsync();
            if (existingAirports.Any())
            {
                return; // Database already seeded
            }

            // Seed Airports
            var airport1 = await unitOfWork.Airports.AddAsync(new Airport("Henri Coandă International Airport", "OTP"));
            var airport2 = await unitOfWork.Airports.AddAsync(new Airport("John F. Kennedy International Airport", "JFK"));
            var airport3 = await unitOfWork.Airports.AddAsync(new Airport("Heathrow Airport", "LHR"));
            var airport4 = await unitOfWork.Airports.AddAsync(new Airport("Charles de Gaulle Airport", "CDG"));
            var airport5 = await unitOfWork.Airports.AddAsync(new Airport("Frankfurt Airport", "FRA"));

            // Save airports first to get their IDs
            await unitOfWork.SaveChangesAsync();

            // Refresh to get IDs (they should be set by EF Core after SaveChanges)
            // Seed Flights using the airport IDs
            await unitOfWork.Flights.AddAsync(new Flight("RO123", airport1.Id));
            await unitOfWork.Flights.AddAsync(new Flight("LH456", airport5.Id));
            await unitOfWork.Flights.AddAsync(new Flight("WZ789", airport1.Id));
            await unitOfWork.Flights.AddAsync(new Flight("AA101", airport2.Id));
            await unitOfWork.Flights.AddAsync(new Flight("BA202", airport3.Id));
            await unitOfWork.Flights.AddAsync(new Flight("AF303", airport4.Id));
            await unitOfWork.Flights.AddAsync(new Flight("RO456", airport1.Id));
            await unitOfWork.Flights.AddAsync(new Flight("LH789", airport5.Id));

            // Seed Passengers
            await unitOfWork.Passengers.AddAsync(new Passenger("John", "Doe", "john.doe@email.com", "+1-555-0101"));
            await unitOfWork.Passengers.AddAsync(new Passenger("Jane", "Smith", "jane.smith@email.com", "+1-555-0102"));
            await unitOfWork.Passengers.AddAsync(new Passenger("Ion", "Popescu", "ion.popescu@email.com", "+40-721-123456"));
            await unitOfWork.Passengers.AddAsync(new Passenger("Maria", "Ionescu", "maria.ionescu@email.com", "+40-722-234567"));
            await unitOfWork.Passengers.AddAsync(new Passenger("Michael", "Johnson", "michael.johnson@email.com", "+1-555-0103"));
            await unitOfWork.Passengers.AddAsync(new Passenger("Sarah", "Williams", "sarah.williams@email.com", "+1-555-0104"));
            await unitOfWork.Passengers.AddAsync(new Passenger("David", "Brown", "david.brown@email.com", null));
            await unitOfWork.Passengers.AddAsync(new Passenger("Emily", "Davis", "emily.davis@email.com", "+44-20-7946-0958"));

            // Save all changes
            await unitOfWork.SaveChangesAsync();
        }
    }
}
