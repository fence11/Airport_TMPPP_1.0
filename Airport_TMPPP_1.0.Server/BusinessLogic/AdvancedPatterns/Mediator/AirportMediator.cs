using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Mediator
{
    // ── Mediator interface ────────────────────────────────────────────────────

    public interface IAirportMediator
    {
        Task<CoordinationResult> CoordinateAsync(AirportServiceRequest request, IUnitOfWork uow);
        IReadOnlyList<ServiceLogEntry> GetLog();
    }

    // ── Request & result models ───────────────────────────────────────────────

    public sealed class AirportServiceRequest
    {
        public string  OperationType { get; init; } = string.Empty; // "PassengerArrival" | "FlightDeparture" | "EmergencyAlert"
        public int     FlightId      { get; init; }
        public string  FlightNumber  { get; init; } = string.Empty;
        public string? PassengerEmail{ get; init; }
        public string? GateCode      { get; init; }
        public string? Note          { get; init; }
    }

    public sealed record ServiceLogEntry(
        string ServiceName,
        string Operation,
        bool   Success,
        string Message,
        DateTime OccurredAtUtc);

    public sealed class CoordinationResult
    {
        public bool   Success       { get; init; }
        public string OperationType { get; init; } = string.Empty;
        public string Summary       { get; init; } = string.Empty;
        public IReadOnlyList<ServiceLogEntry> ServiceLog { get; init; } = Array.Empty<ServiceLogEntry>();
    }

    // ── Colleague interface ───────────────────────────────────────────────────

    public interface IAirportService
    {
        string ServiceName { get; }
        Task<ServiceLogEntry> ProcessAsync(AirportServiceRequest request, IUnitOfWork uow);
    }

    // ── Concrete services (colleagues) ────────────────────────────────────────

    public sealed class CheckInService : IAirportService
    {
        public string ServiceName => "CheckIn";

        public async Task<ServiceLogEntry> ProcessAsync(AirportServiceRequest request, IUnitOfWork uow)
        {
            bool hasPassenger = false;
            string msg;

            if (request.PassengerEmail is not null)
            {
                var passengers = await uow.Passengers.GetAllAsync();
                var p = passengers.FirstOrDefault(x =>
                    x.Email.Equals(request.PassengerEmail, StringComparison.OrdinalIgnoreCase));
                hasPassenger = p is not null;
                msg = hasPassenger
                    ? $"Check-in processed for {p!.FirstName} {p.LastName} on flight {request.FlightNumber}."
                    : $"Passenger '{request.PassengerEmail}' not found — walk-in check-in initiated.";
            }
            else
            {
                msg = $"Bulk check-in cleared for flight {request.FlightNumber}.";
                hasPassenger = true;
            }

            return new ServiceLogEntry(ServiceName, request.OperationType, hasPassenger || true, msg, DateTime.UtcNow);
        }
    }

    public sealed class SecurityService : IAirportService
    {
        public string ServiceName => "Security";

        public async Task<ServiceLogEntry> ProcessAsync(AirportServiceRequest request, IUnitOfWork uow)
        {
            // Touch DB: verify the flight airport exists (security clearance per airport)
            var flight = await uow.Flights.GetByIdAsync(request.FlightId);
            bool ok = flight is not null;

            string msg = ok
                ? $"Security clearance granted for flight {request.FlightNumber} at airport #{flight!.AirportId}."
                : $"Security alert: flight {request.FlightNumber} not verified in database.";

            return new ServiceLogEntry(ServiceName, request.OperationType, ok, msg, DateTime.UtcNow);
        }
    }

    public sealed class GateManagementService : IAirportService
    {
        public string ServiceName => "GateManagement";

        public async Task<ServiceLogEntry> ProcessAsync(AirportServiceRequest request, IUnitOfWork uow)
        {
            var flight = await uow.Flights.GetByIdAsync(request.FlightId);
            bool ok = flight is not null;
            string gate = request.GateCode ?? $"G-{(flight?.Id ?? 0) % 20 + 1:D2}";

            string msg = ok
                ? $"Gate {gate} assigned to flight {request.FlightNumber}. Boarding agents notified."
                : $"Gate assignment failed — flight {request.FlightNumber} not found.";

            return new ServiceLogEntry(ServiceName, request.OperationType, ok, msg, DateTime.UtcNow);
        }
    }

    public sealed class BaggageHandlingService : IAirportService
    {
        public string ServiceName => "BaggageHandling";

        public async Task<ServiceLogEntry> ProcessAsync(AirportServiceRequest request, IUnitOfWork uow)
        {
            var flight = await uow.Flights.GetByIdAsync(request.FlightId);
            bool ok = flight is not null;

            string msg = ok
                ? $"Baggage carousel activated for flight {request.FlightNumber}. Handlers dispatched."
                : $"Baggage handling skipped — flight {request.FlightNumber} unknown.";

            return new ServiceLogEntry(ServiceName, request.OperationType, ok, msg, DateTime.UtcNow);
        }
    }

    public sealed class AirTrafficControlService : IAirportService
    {
        public string ServiceName => "AirTrafficControl";

        public async Task<ServiceLogEntry> ProcessAsync(AirportServiceRequest request, IUnitOfWork uow)
        {
            var flight = await uow.Flights.GetByIdAsync(request.FlightId);
            bool ok = flight is not null;

            string msg = ok
                ? $"ATC slot confirmed for flight {request.FlightNumber}. Runway sequence updated."
                : $"ATC could not confirm slot — flight {request.FlightNumber} not on register.";

            return new ServiceLogEntry(ServiceName, request.OperationType, ok, msg, DateTime.UtcNow);
        }
    }

    // ── Concrete mediator ─────────────────────────────────────────────────────

    public sealed class AirportCoordinationMediator : IAirportMediator
    {
        private readonly CheckInService          _checkIn   = new();
        private readonly SecurityService         _security  = new();
        private readonly GateManagementService   _gate      = new();
        private readonly BaggageHandlingService  _baggage   = new();
        private readonly AirTrafficControlService _atc      = new();

        private readonly List<ServiceLogEntry> _globalLog = new();

        public async Task<CoordinationResult> CoordinateAsync(AirportServiceRequest request, IUnitOfWork uow)
        {
            var log = new List<ServiceLogEntry>();

            // Route to the right set of services based on operation type
            IEnumerable<IAirportService> services = request.OperationType.ToLower() switch
            {
                "passengerarrival"  => new IAirportService[] { _checkIn, _security, _baggage },
                "flightdeparture"   => new IAirportService[] { _checkIn, _security, _gate, _atc },
                "emergencyalert"    => new IAirportService[] { _security, _atc, _gate },
                "baggageclaim"      => new IAirportService[] { _baggage },
                _                   => new IAirportService[] { _checkIn, _security, _gate, _baggage, _atc }
            };

            foreach (var svc in services)
            {
                var entry = await svc.ProcessAsync(request, uow);
                log.Add(entry);
                _globalLog.Insert(0, entry);
            }

            if (_globalLog.Count > 200) _globalLog.RemoveRange(200, _globalLog.Count - 200);

            bool allOk = log.All(e => e.Success);
            return new CoordinationResult
            {
                Success       = allOk,
                OperationType = request.OperationType,
                Summary       = allOk
                    ? $"All {log.Count} services processed '{request.OperationType}' successfully for flight {request.FlightNumber}."
                    : $"'{request.OperationType}' completed with issues on flight {request.FlightNumber}. Check log.",
                ServiceLog    = log.AsReadOnly()
            };
        }

        public IReadOnlyList<ServiceLogEntry> GetLog() => _globalLog.AsReadOnly();
    }
}
