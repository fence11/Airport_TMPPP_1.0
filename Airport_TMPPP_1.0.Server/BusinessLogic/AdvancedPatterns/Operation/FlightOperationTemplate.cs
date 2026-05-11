using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.TemplateMethod
{
    // ── Step record ───────────────────────────────────────────────────────────

    public sealed record OperationStep(
        string StepName,
        bool   Completed,
        string Result,
        DateTime ExecutedAtUtc);

    public sealed class FlightOperationReport
    {
        public string   OperationName   { get; init; } = string.Empty;
        public string   FlightNumber    { get; init; } = string.Empty;
        public int      FlightId        { get; init; }
        public bool     OverallSuccess  { get; init; }
        public string   Summary         { get; init; } = string.Empty;
        public IReadOnlyList<OperationStep> Steps { get; init; } = Array.Empty<OperationStep>();
    }

    // ── Abstract template ─────────────────────────────────────────────────────

    public abstract class FlightOperationTemplate
    {
        public abstract string OperationName { get; }

        // Template method — defines the fixed skeleton
        public async Task<FlightOperationReport> RunAsync(int flightId, IUnitOfWork uow)
        {
            var steps = new List<OperationStep>();

            // Step 1 – always: validate flight exists (invariant)
            var validateStep = await ValidateFlightAsync(flightId, uow);
            steps.Add(validateStep);
            if (!validateStep.Completed)
                return BuildReport(flightId, "UNKNOWN", false, steps);

            var flight = (await uow.Flights.GetByIdAsync(flightId))!;

            // Step 2 – hook: pre-operation check (subclass-specific)
            steps.Add(await PreOperationCheckAsync(flight.FlightNumber, flightId, uow));

            // Step 3 – hook: main operation body (subclass-specific)
            steps.Add(await ExecuteCoreOperationAsync(flight.FlightNumber, flightId, uow));

            // Step 4 – hook: post-operation procedure (subclass-specific)
            steps.Add(await PostOperationProcedureAsync(flight.FlightNumber, flightId, uow));

            // Step 5 – always: log completion (invariant, writes UpdatedAt to DB)
            steps.Add(await LogCompletionAsync(flight.FlightNumber, flightId, uow));

            bool ok = steps.All(s => s.Completed);
            return BuildReport(flightId, flight.FlightNumber, ok, steps);
        }

        // ── Invariant step ────────────────────────────────────────────────────

        private static async Task<OperationStep> ValidateFlightAsync(int flightId, IUnitOfWork uow)
        {
            var flight = await uow.Flights.GetByIdAsync(flightId);
            return new OperationStep(
                "FlightValidation",
                flight is not null,
                flight is not null
                    ? $"Flight #{flightId} ({flight.FlightNumber}) confirmed in database."
                    : $"Flight #{flightId} not found. Operation aborted.",
                DateTime.UtcNow);
        }

        private static async Task<OperationStep> LogCompletionAsync(
            string flightNumber, int flightId, IUnitOfWork uow)
        {
            // Prove DB write: touch the flight's UpdatedAt via the airport lookup
            var flight = await uow.Flights.GetByIdAsync(flightId);
            if (flight is not null)
            {
                flight.MarkUpdated();
                await uow.Flights.UpdateAsync(flight);
            }
            return new OperationStep(
                "CompletionLog",
                true,
                $"Operation completion logged to DB. Flight {flightNumber} UpdatedAt refreshed.",
                DateTime.UtcNow);
        }

        // ── Abstract hooks (subclasses fill in) ───────────────────────────────

        protected abstract Task<OperationStep> PreOperationCheckAsync(
            string flightNumber, int flightId, IUnitOfWork uow);

        protected abstract Task<OperationStep> ExecuteCoreOperationAsync(
            string flightNumber, int flightId, IUnitOfWork uow);

        protected abstract Task<OperationStep> PostOperationProcedureAsync(
            string flightNumber, int flightId, IUnitOfWork uow);

        // ── Helper ────────────────────────────────────────────────────────────

        private FlightOperationReport BuildReport(
            int flightId, string flightNumber, bool ok, List<OperationStep> steps) =>
            new()
            {
                OperationName  = OperationName,
                FlightId       = flightId,
                FlightNumber   = flightNumber,
                OverallSuccess = ok,
                Summary        = ok
                    ? $"{OperationName} completed successfully for flight {flightNumber}."
                    : $"{OperationName} failed for flight {flightNumber}. See step log.",
                Steps          = steps.AsReadOnly()
            };
    }

    // ── Concrete templates ────────────────────────────────────────────────────

    /// Pre-flight checks: crew readiness, fuel, weather clearance.
    public sealed class PreFlightOperation : FlightOperationTemplate
    {
        public override string OperationName => "PreFlightChecks";

        protected override async Task<OperationStep> PreOperationCheckAsync(
            string flightNumber, int flightId, IUnitOfWork uow)
        {
            var airport = await GetAirportAsync(flightId, uow);
            return new OperationStep(
                "CrewAndAircraftReadiness",
                true,
                $"Crew manifest verified. Aircraft assigned to gate at {airport}. All systems nominal.",
                DateTime.UtcNow);
        }

        protected override Task<OperationStep> ExecuteCoreOperationAsync(
            string flightNumber, int flightId, IUnitOfWork uow) =>
            Task.FromResult(new OperationStep(
                "FuelAndWeatherCheck",
                true,
                $"Fuel load: 42,000 kg ✓  |  Weather: VFR conditions ✓  |  NOTAM reviewed ✓",
                DateTime.UtcNow));

        protected override Task<OperationStep> PostOperationProcedureAsync(
            string flightNumber, int flightId, IUnitOfWork uow) =>
            Task.FromResult(new OperationStep(
                "PilotSignOff",
                true,
                $"Captain sign-off complete. Flight {flightNumber} cleared for boarding.",
                DateTime.UtcNow));
    }

    /// Post-flight procedures: landing, docking, passenger disembark.
    public sealed class PostFlightOperation : FlightOperationTemplate
    {
        public override string OperationName => "PostFlightProcedures";

        protected override async Task<OperationStep> PreOperationCheckAsync(
            string flightNumber, int flightId, IUnitOfWork uow)
        {
            var airport = await GetAirportAsync(flightId, uow);
            return new OperationStep(
                "ArrivalGateAllocation",
                true,
                $"Arrival gate allocated at {airport}. Ground crew on standby.",
                DateTime.UtcNow);
        }

        protected override Task<OperationStep> ExecuteCoreOperationAsync(
            string flightNumber, int flightId, IUnitOfWork uow) =>
            Task.FromResult(new OperationStep(
                "PassengerDisembarkation",
                true,
                $"Jetway connected. Disembarkation started. Estimated 25 min to clear all passengers.",
                DateTime.UtcNow));

        protected override Task<OperationStep> PostOperationProcedureAsync(
            string flightNumber, int flightId, IUnitOfWork uow) =>
            Task.FromResult(new OperationStep(
                "AircraftTurnaround",
                true,
                $"Cleaning crew dispatched. Catering restocked. Flight {flightNumber} ready for next rotation.",
                DateTime.UtcNow));
    }

    /// Emergency evacuation procedure.
    public sealed class EmergencyEvacuationOperation : FlightOperationTemplate
    {
        public override string OperationName => "EmergencyEvacuation";

        protected override async Task<OperationStep> PreOperationCheckAsync(
            string flightNumber, int flightId, IUnitOfWork uow)
        {
            // DB read: confirm airport emergency contacts
            var flight = await uow.Flights.GetByIdAsync(flightId);
            var airport = flight is not null ? await uow.Airports.GetByIdAsync(flight.AirportId) : null;
            return new OperationStep(
                "EmergencyServicesAlert",
                true,
                $"Fire service, medical, and security alerted at {airport?.Name ?? "airport"}. ETA 4 min.",
                DateTime.UtcNow);
        }

        protected override Task<OperationStep> ExecuteCoreOperationAsync(
            string flightNumber, int flightId, IUnitOfWork uow) =>
            Task.FromResult(new OperationStep(
                "EvacuationExecution",
                true,
                $"All exits deployed. Crew directing passengers. PA announcement broadcast.",
                DateTime.UtcNow));

        protected override Task<OperationStep> PostOperationProcedureAsync(
            string flightNumber, int flightId, IUnitOfWork uow) =>
            Task.FromResult(new OperationStep(
                "IncidentReport",
                true,
                $"Headcount complete. Incident report filed. Flight {flightNumber} grounded pending investigation.",
                DateTime.UtcNow));
    }

    // ── Shared helper ─────────────────────────────────────────────────────────

    internal static class TemplateHelpers
    {
        internal static async Task<string> GetAirportAsync(int flightId, IUnitOfWork uow)
        {
            var flight = await uow.Flights.GetByIdAsync(flightId);
            if (flight is null) return "Unknown Airport";
            var airport = await uow.Airports.GetByIdAsync(flight.AirportId);
            return airport is not null ? $"{airport.Name} ({airport.Code})" : "Unknown Airport";
        }
    }

    // Extension to use helper from template methods without inheritance complexity
    public abstract partial class FlightOperationTemplate
    {
        protected static Task<string> GetAirportAsync(int flightId, IUnitOfWork uow) =>
            TemplateHelpers.GetAirportAsync(flightId, uow);
    }
}
