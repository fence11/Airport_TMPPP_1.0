using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.ChainOfResponsibility;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.State;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Mediator;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.TemplateMethod;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Visitor;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Airport_TMPPP_1._0.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdvancedPatternsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        // Shared long-lived instances
        private static readonly FlightStateRegistry       _stateRegistry = new();
        private static readonly AirportCoordinationMediator _mediator    = new();

        public AdvancedPatternsController(IUnitOfWork uow) => _uow = uow;

        // ── CHAIN OF RESPONSIBILITY ───────────────────────────────────────────

        /// <summary>
        /// Runs a booking request through the handler chain:
        /// FlightExists → PassengerValidation → BaggageCheck → PaymentValidation → BookingConfirmation
        /// </summary>
        [HttpPost("chain/book")]
        public async Task<IActionResult> ProcessBooking([FromBody] BookingRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PassengerEmail))
                return BadRequest("PassengerEmail is required.");
            if (string.IsNullOrWhiteSpace(dto.FlightNumber))
                return BadRequest("FlightNumber is required.");

            var request = new BookingRequest
            {
                PassengerName   = dto.PassengerName,
                PassengerEmail  = dto.PassengerEmail,
                FlightId        = dto.FlightId,
                FlightNumber    = dto.FlightNumber,
                BaggageWeightKg = dto.BaggageWeightKg,
                HasSpecialMeal  = dto.HasSpecialMeal,
                PaymentMethod   = dto.PaymentMethod,
                TicketPrice     = dto.TicketPrice
            };

            var result = await BookingChainBuilder.ProcessAsync(request, _uow);
            return result.Success ? Ok(result) : UnprocessableEntity(result);
        }

        /// <summary>Returns the available passengers and flights to help build test requests.</summary>
        [HttpGet("chain/context")]
        public async Task<IActionResult> GetChainContext()
        {
            var flights    = (await _uow.Flights.GetAllAsync())
                .Select(f => new { f.Id, f.FlightNumber, f.AirportId })
                .ToList();
            var passengers = (await _uow.Passengers.GetAllAsync())
                .Select(p => new { p.Id, p.FirstName, p.LastName, p.Email })
                .ToList();

            return Ok(new { Flights = flights, Passengers = passengers });
        }

        // ── STATE ─────────────────────────────────────────────────────────────

        /// <summary>Returns (or creates) the state context for a flight.</summary>
        [HttpGet("state/flight/{flightId:int}")]
        public async Task<IActionResult> GetFlightState(int flightId)
        {
            try
            {
                var ctx = await _stateRegistry.GetOrCreateAsync(flightId, _uow);
                return Ok(StateContextDto.From(ctx));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>Returns all tracked flight state contexts.</summary>
        [HttpGet("state/all")]
        public async Task<IActionResult> GetAllFlightStates()
        {
            // Seed any DB flights not yet in registry
            var flights = await _uow.Flights.GetAllAsync();
            foreach (var f in flights.Take(8))
                await _stateRegistry.GetOrCreateAsync(f.Id, _uow);

            var dtos = _stateRegistry.All.Values.Select(StateContextDto.From).ToList();
            return Ok(dtos);
        }

        /// <summary>Fires a transition action on a flight state machine.</summary>
        [HttpPost("state/flight/{flightId:int}/transition")]
        public async Task<IActionResult> TransitionFlightState(
            int flightId, [FromBody] TransitionRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Action))
                return BadRequest("Action is required.");

            FlightContext ctx;
            try { ctx = await _stateRegistry.GetOrCreateAsync(flightId, _uow); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }

            string actor = dto.Actor ?? "System";
            string? note = dto.Note;

            StateActionResult result = dto.Action.ToLower() switch
            {
                "opencheckin"   => ctx.OpenCheckIn(actor, note),
                "startboarding" => ctx.StartBoarding(actor, note),
                "depart"        => ctx.Depart(actor, note),
                "markinair"     => ctx.MarkInAir(actor, note),
                "land"          => ctx.Land(actor, note),
                "delay"         => ctx.Delay(actor, note ?? "Operational reasons"),
                "cancel"        => ctx.Cancel(actor, note ?? "Operational reasons"),
                "reschedule"    => ctx.Reschedule(actor, note),
                _ => new StateActionResult(false, $"Unknown action '{dto.Action}'.", null)
            };

            return result.Success
                ? Ok(new { result, Context = StateContextDto.From(ctx) })
                : UnprocessableEntity(result);
        }

        [HttpGet("state/actions")]
        public IActionResult GetAllActions() => Ok(new[]
        {
            "OpenCheckIn","StartBoarding","Depart","MarkInAir","Land","Delay","Cancel","Reschedule"
        });

        // ── MEDIATOR ──────────────────────────────────────────────────────────

        [HttpPost("mediator/coordinate")]
        public async Task<IActionResult> CoordinateServices([FromBody] CoordinationRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OperationType))
                return BadRequest("OperationType is required.");

            var request = new AirportServiceRequest
            {
                OperationType  = dto.OperationType,
                FlightId       = dto.FlightId,
                FlightNumber   = dto.FlightNumber,
                PassengerEmail = dto.PassengerEmail,
                GateCode       = dto.GateCode,
                Note           = dto.Note
            };

            var result = await _mediator.CoordinateAsync(request, _uow);
            return result.Success ? Ok(result) : Ok(result); // always 200 — let UI decide rendering
        }

        [HttpGet("mediator/log")]
        public IActionResult GetMediatorLog() =>
            Ok(_mediator.GetLog().Take(30));

        [HttpGet("mediator/operation-types")]
        public IActionResult GetOperationTypes() => Ok(new[]
        {
            new { Key = "PassengerArrival",  Label = "Passenger Arrival",  Services = new[]{"CheckIn","Security","BaggageHandling"} },
            new { Key = "FlightDeparture",   Label = "Flight Departure",   Services = new[]{"CheckIn","Security","GateManagement","AirTrafficControl"} },
            new { Key = "EmergencyAlert",    Label = "Emergency Alert",    Services = new[]{"Security","AirTrafficControl","GateManagement"} },
            new { Key = "BaggageClaim",      Label = "Baggage Claim",      Services = new[]{"BaggageHandling"} },
            new { Key = "FullCoordination",  Label = "Full Coordination",  Services = new[]{"CheckIn","Security","GateManagement","BaggageHandling","AirTrafficControl"} }
        });

        // ── TEMPLATE METHOD ───────────────────────────────────────────────────

        [HttpPost("template/run")]
        public async Task<IActionResult> RunFlightOperation([FromBody] TemplateRunRequestDto dto)
        {
            FlightOperationTemplate op = dto.OperationType?.ToLower() switch
            {
                "preflight"   => new PreFlightOperation(),
                "postflight"  => new PostFlightOperation(),
                "emergency"   => new EmergencyEvacuationOperation(),
                _ => new PreFlightOperation()
            };

            var report = await op.RunAsync(dto.FlightId, _uow);
            return report.OverallSuccess ? Ok(report) : UnprocessableEntity(report);
        }

        [HttpGet("template/operation-types")]
        public IActionResult GetTemplateTypes() => Ok(new[]
        {
            new { Key = "preflight",  Label = "Pre-Flight Checks",        Description = "Crew, fuel, weather, pilot sign-off." },
            new { Key = "postflight", Label = "Post-Flight Procedures",   Description = "Gate allocation, disembarkation, turnaround." },
            new { Key = "emergency",  Label = "Emergency Evacuation",     Description = "Alert services, evacuate, file incident report." }
        });

        // ── VISITOR ───────────────────────────────────────────────────────────

        [HttpGet("visitor/report")]
        public async Task<IActionResult> GenerateReport([FromQuery] string type = "statistics")
        {
            var airports   = await _uow.Airports.GetAllAsync();
            var flights    = await _uow.Flights.GetAllAsync();
            var passengers = await _uow.Passengers.GetAllAsync();

            var structure = AirportObjectStructure.Build(airports, flights, passengers);

            VisitorReport report = type.ToLower() switch
            {
                "audit"   => RunVisitor<AuditVisitor>(structure, v => v.ToReport()),
                "contact" => RunVisitor<ContactExportVisitor>(structure, v => v.ToReport()),
                _         => RunVisitor<StatisticsVisitor>(structure, v => v.ToReport())
            };

            return Ok(report);
        }

        [HttpGet("visitor/report-types")]
        public IActionResult GetReportTypes() => Ok(new[]
        {
            new { Key = "statistics", Label = "Statistics",     Description = "Counts, totals, busiest airport." },
            new { Key = "audit",      Label = "Audit / Compliance", Description = "Validates all records for completeness." },
            new { Key = "contact",    Label = "Contact Export", Description = "Exports passenger contact info." }
        });

        private static VisitorReport RunVisitor<TVisitor>(
            AirportObjectStructure structure,
            Func<TVisitor, VisitorReport> extract)
            where TVisitor : IAirportVisitor, new()
        {
            var visitor = new TVisitor();
            structure.AcceptAll(visitor);
            return extract(visitor);
        }
    }

    // ── Request / Response DTOs ───────────────────────────────────────────────

    public sealed class BookingRequestDto
    {
        public string  PassengerName   { get; set; } = string.Empty;
        public string  PassengerEmail  { get; set; } = string.Empty;
        public int     FlightId        { get; set; }
        public string  FlightNumber    { get; set; } = string.Empty;
        public int     BaggageWeightKg { get; set; } = 20;
        public bool    HasSpecialMeal  { get; set; }
        public string  PaymentMethod   { get; set; } = "CreditCard";
        public decimal TicketPrice     { get; set; } = 350m;
    }

    public sealed class TransitionRequestDto
    {
        public string  Action { get; set; } = string.Empty;
        public string? Actor  { get; set; }
        public string? Note   { get; set; }
    }

    public sealed class CoordinationRequestDto
    {
        public string  OperationType  { get; set; } = "FlightDeparture";
        public int     FlightId       { get; set; }
        public string  FlightNumber   { get; set; } = string.Empty;
        public string? PassengerEmail { get; set; }
        public string? GateCode       { get; set; }
        public string? Note           { get; set; }
    }

    public sealed class TemplateRunRequestDto
    {
        public string OperationType { get; set; } = "preflight";
        public int    FlightId      { get; set; }
    }

    public sealed class StateContextDto
    {
        public int    FlightId            { get; init; }
        public string FlightNumber        { get; init; } = string.Empty;
        public string CurrentStatus       { get; init; } = string.Empty;
        public string StatusLabel         { get; init; } = string.Empty;
        public string StatusDescription   { get; init; } = string.Empty;
        public IReadOnlyList<string> AllowedTransitions { get; init; } = Array.Empty<string>();
        public IReadOnlyList<StateTransitionRecord> History { get; init; } = Array.Empty<StateTransitionRecord>();

        public static StateContextDto From(FlightContext ctx) => new()
        {
            FlightId          = ctx.FlightId,
            FlightNumber      = ctx.FlightNumber,
            CurrentStatus     = ctx.CurrentStatus.ToString(),
            StatusLabel       = ctx.StatusLabel,
            StatusDescription = ctx.StatusDescription,
            AllowedTransitions= ctx.AllowedTransitions,
            History           = ctx.History
        };
    }
}
