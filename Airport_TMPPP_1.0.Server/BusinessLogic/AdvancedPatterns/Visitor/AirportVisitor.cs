using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Visitor
{
    // ── Element interface ─────────────────────────────────────────────────────

    public interface IAirportElement
    {
        void Accept(IAirportVisitor visitor);
    }

    // ── Visitor interface ─────────────────────────────────────────────────────

    public interface IAirportVisitor
    {
        void Visit(AirportElement element);
        void Visit(FlightElement element);
        void Visit(PassengerElement element);
    }

    // ── Element wrappers (thin, non-modifying adapters) ───────────────────────

    public sealed class AirportElement : IAirportElement
    {
        public Airport Airport     { get; }
        public int     FlightCount { get; }

        public AirportElement(Airport airport, int flightCount)
        {
            Airport     = airport;
            FlightCount = flightCount;
        }

        public void Accept(IAirportVisitor visitor) => visitor.Visit(this);
    }

    public sealed class FlightElement : IAirportElement
    {
        public Flight  Flight      { get; }
        public string  AirportCode { get; }
        public string  AirportName { get; }

        public FlightElement(Flight flight, string airportCode, string airportName)
        {
            Flight      = flight;
            AirportCode = airportCode;
            AirportName = airportName;
        }

        public void Accept(IAirportVisitor visitor) => visitor.Visit(this);
    }

    public sealed class PassengerElement : IAirportElement
    {
        public Passenger Passenger { get; }
        public bool      HasPhone  { get; }

        public PassengerElement(Passenger passenger)
        {
            Passenger = passenger;
            HasPhone  = !string.IsNullOrWhiteSpace(passenger.PhoneNumber);
        }

        public void Accept(IAirportVisitor visitor) => visitor.Visit(this);
    }

    // ── Concrete visitor 1: Statistics collector ───────────────────────────────

    public sealed class StatisticsVisitor : IAirportVisitor
    {
        public int   TotalAirports         { get; private set; }
        public int   TotalFlights          { get; private set; }
        public int   TotalPassengers       { get; private set; }
        public int   PassengersWithPhone   { get; private set; }
        public int   PassengersWithoutPhone{ get; private set; }
        public int   TotalFlightCapacity   { get; private set; } // flights × assumed 180 seats
        public int   MaxFlightsAtAirport   { get; private set; }
        public string BusiestAirport       { get; private set; } = string.Empty;

        private int _currentFlightCount;

        public void Visit(AirportElement e)
        {
            TotalAirports++;
            _currentFlightCount = e.FlightCount;
            TotalFlightCapacity += e.FlightCount * 180;
            if (e.FlightCount > MaxFlightsAtAirport)
            {
                MaxFlightsAtAirport = e.FlightCount;
                BusiestAirport = $"{e.Airport.Name} ({e.Airport.Code})";
            }
        }

        public void Visit(FlightElement e) => TotalFlights++;

        public void Visit(PassengerElement e)
        {
            TotalPassengers++;
            if (e.HasPhone) PassengersWithPhone++;
            else            PassengersWithoutPhone++;
        }

        public VisitorReport ToReport() => new()
        {
            ReportType   = "Statistics",
            GeneratedAtUtc = DateTime.UtcNow,
            Rows = new[]
            {
                new ReportRow("Total Airports",          TotalAirports.ToString()),
                new ReportRow("Total Flights",           TotalFlights.ToString()),
                new ReportRow("Total Passengers",        TotalPassengers.ToString()),
                new ReportRow("Passengers with Phone",   PassengersWithPhone.ToString()),
                new ReportRow("Passengers without Phone",PassengersWithoutPhone.ToString()),
                new ReportRow("Total Seat Capacity",     TotalFlightCapacity.ToString()),
                new ReportRow("Busiest Airport",         BusiestAirport),
                new ReportRow("Max Flights at Airport",  MaxFlightsAtAirport.ToString()),
            }
        };
    }

    // ── Concrete visitor 2: Audit / compliance report ─────────────────────────

    public sealed class AuditVisitor : IAirportVisitor
    {
        private readonly List<ReportRow> _rows = new();
        private int _airportsMissingCode;
        private int _flightsMissingAirport;
        private int _passengersNoContact;

        public void Visit(AirportElement e)
        {
            bool codeOk = !string.IsNullOrWhiteSpace(e.Airport.Code) && e.Airport.Code.Length >= 2;
            if (!codeOk) _airportsMissingCode++;
            _rows.Add(new ReportRow(
                $"Airport: {e.Airport.Name}",
                codeOk
                    ? $"✓ Code '{e.Airport.Code}' valid | {e.FlightCount} flights"
                    : $"✗ Airport code '{e.Airport.Code}' too short or missing"));
        }

        public void Visit(FlightElement e)
        {
            bool ok = !string.IsNullOrWhiteSpace(e.Flight.FlightNumber) && !string.IsNullOrWhiteSpace(e.AirportCode);
            if (!ok) _flightsMissingAirport++;
            _rows.Add(new ReportRow(
                $"Flight: {e.Flight.FlightNumber}",
                ok
                    ? $"✓ Assigned to {e.AirportName} ({e.AirportCode})"
                    : $"✗ Missing airport assignment"));
        }

        public void Visit(PassengerElement e)
        {
            bool ok = !string.IsNullOrWhiteSpace(e.Passenger.Email);
            bool hasContact = ok || e.HasPhone;
            if (!hasContact) _passengersNoContact++;
            _rows.Add(new ReportRow(
                $"Passenger: {e.Passenger.FirstName} {e.Passenger.LastName}",
                hasContact
                    ? $"✓ Email: {e.Passenger.Email}{(e.HasPhone ? " | Phone: " + e.Passenger.PhoneNumber : "")}"
                    : $"✗ No contact details on record"));
        }

        public VisitorReport ToReport() => new()
        {
            ReportType     = "Audit",
            GeneratedAtUtc = DateTime.UtcNow,
            Summary = $"Airports missing code: {_airportsMissingCode} | " +
                      $"Flights missing airport: {_flightsMissingAirport} | " +
                      $"Passengers no contact: {_passengersNoContact}",
            Rows = _rows.AsReadOnly()
        };
    }

    // ── Concrete visitor 3: Contact export ────────────────────────────────────

    public sealed class ContactExportVisitor : IAirportVisitor
    {
        private readonly List<ReportRow> _contacts = new();

        public void Visit(AirportElement e) { /* not relevant for contacts */ }
        public void Visit(FlightElement e)  { /* not relevant for contacts */ }

        public void Visit(PassengerElement e)
        {
            _contacts.Add(new ReportRow(
                $"{e.Passenger.FirstName} {e.Passenger.LastName}",
                $"{e.Passenger.Email}{(e.HasPhone ? " | " + e.Passenger.PhoneNumber : " | (no phone)")}"));
        }

        public VisitorReport ToReport() => new()
        {
            ReportType     = "ContactExport",
            GeneratedAtUtc = DateTime.UtcNow,
            Summary        = $"{_contacts.Count} passenger contact records exported.",
            Rows           = _contacts.AsReadOnly()
        };
    }

    // ── Report model ──────────────────────────────────────────────────────────

    public sealed class VisitorReport
    {
        public string   ReportType     { get; init; } = string.Empty;
        public DateTime GeneratedAtUtc { get; init; }
        public string?  Summary        { get; init; }
        public IReadOnlyList<ReportRow> Rows { get; init; } = Array.Empty<ReportRow>();
    }

    public sealed record ReportRow(string Label, string Value);

    // ── Object structure (composite of all elements, built from DB data) ───────

    public sealed class AirportObjectStructure
    {
        private readonly List<IAirportElement> _elements = new();

        public void Add(IAirportElement element) => _elements.Add(element);

        public void AcceptAll(IAirportVisitor visitor)
        {
            foreach (var el in _elements)
                el.Accept(visitor);
        }

        public static AirportObjectStructure Build(
            IEnumerable<Airport>   airports,
            IEnumerable<Flight>    flights,
            IEnumerable<Passenger> passengers)
        {
            var structure = new AirportObjectStructure();
            var airportMap    = airports.ToDictionary(a => a.Id);
            var flightsByAirport = flights.GroupBy(f => f.AirportId)
                                          .ToDictionary(g => g.Key, g => g.Count());

            foreach (var airport in airportMap.Values)
            {
                flightsByAirport.TryGetValue(airport.Id, out int count);
                structure.Add(new AirportElement(airport, count));
            }

            foreach (var flight in flights)
            {
                airportMap.TryGetValue(flight.AirportId, out var ap);
                structure.Add(new FlightElement(flight, ap?.Code ?? "???", ap?.Name ?? "Unknown"));
            }

            foreach (var passenger in passengers)
                structure.Add(new PassengerElement(passenger));

            return structure;
        }
    }
}
