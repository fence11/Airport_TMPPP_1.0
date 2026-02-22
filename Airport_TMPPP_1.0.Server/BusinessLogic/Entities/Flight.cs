using Airport_TMPPP_1._0.Server.BusinessLogic.Common;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Entities
{
    public class Flight : AuditableEntity
    {
        public string FlightNumber { get; private set; } = null!;
        public int AirportId { get; private set; }

        private Flight() { } // EF Core

        public Flight(string flightNumber, int airportId)
        {
            FlightNumber = flightNumber;
            AirportId = airportId;
        }
        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(FlightNumber))
                throw new Exception("Flight number required");

            if (AirportId <= 0)
                throw new Exception("AirportId invalid");
        }
    }
}
