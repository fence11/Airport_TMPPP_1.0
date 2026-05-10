using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Command;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Iterator;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Memento;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Observer;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Strategy;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Airport_TMPPP_1._0.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BehavioralPatternsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        // Shared long-lived instances (scoped per-app for demo purposes)
        private static readonly FlightCommandInvoker _commandInvoker = new();
        private static readonly AirportConfigCaretaker _caretaker = new();

        public BehavioralPatternsController(IUnitOfWork uow) => _uow = uow;

        // ── STRATEGY ─────────────────────────────────────────────────────

        [HttpGet("strategy/schedule")]
        public async Task<IActionResult> RunSchedulingStrategy(
            [FromQuery] string strategy = "fcfs",
            [FromQuery] int runwayCount = 2)
        {
            var flights = (await _uow.Flights.GetAllAsync()).ToList();

            var runways = Enumerable.Range(1, Math.Clamp(runwayCount, 1, 5))
                .Select(i => new RunwaySlot(i, $"RW-{i:D2}", true, null))
                .ToList();

            IFlightSchedulingStrategy schedulingStrategy = strategy.ToLower() switch
            {
                "sjf"       => new ShortestJobFirstStrategy(),
                "roundrobin"=> new RoundRobinStrategy(),
                _           => new FirstComeFirstServedStrategy()
            };

            var scheduler = new FlightScheduler(schedulingStrategy);
            var window = DateTime.UtcNow.Date.AddHours(6);
            var scheduled = scheduler.RunSchedule(flights, runways, window, window.AddHours(16)).ToList();

            return Ok(new
            {
                Strategy = scheduler.CurrentStrategy,
                Description = scheduler.CurrentDescription,
                WindowStart = window,
                TotalFlights = flights.Count,
                TotalRunways = runways.Count,
                Schedule = scheduled
            });
        }

        [HttpGet("strategy/strategies")]
        public IActionResult GetAvailableStrategies() => Ok(new[]
        {
            new { Key = "fcfs",       Name = "First-Come First-Served", Description = "Assigns flights in registration order." },
            new { Key = "sjf",        Name = "Shortest Job First",       Description = "Short-turnaround flights take priority." },
            new { Key = "roundrobin", Name = "Round-Robin",              Description = "Distributes evenly across all runways." }
        });

        // ── OBSERVER ─────────────────────────────────────────────────────

        [HttpPost("observer/status-change")]
        public async Task<IActionResult> ChangeFlightStatus([FromBody] ChangeFlightStatusRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.FlightNumber))
                return BadRequest("FlightNumber is required.");

            var flights = await _uow.Flights.GetAllAsync();
            var flight = flights.FirstOrDefault(f =>
                f.FlightNumber.Equals(req.FlightNumber, StringComparison.OrdinalIgnoreCase));

            if (flight is null)
                return NotFound($"Flight {req.FlightNumber} not found.");

            if (!Enum.TryParse<FlightStatus>(req.NewStatus, true, out var newStatus))
                return BadRequest($"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<FlightStatus>())}");

            var oldStatus = req.OldStatus != null && Enum.TryParse<FlightStatus>(req.OldStatus, true, out var os)
                ? os : FlightStatus.Scheduled;

            var evt = new FlightStatusChangedEvent(
                flight.Id,
                flight.FlightNumber,
                oldStatus,
                newStatus,
                req.GateCode,
                req.Reason,
                DateTime.UtcNow);

            FlightStatusEventBus.Shared.NotifyAll(evt);

            var observerLogs = FlightStatusEventBus.Shared.GetObservers()
                .ToDictionary(
                    o => o.ObserverId,
                    o => new { o.ObserverType, Log = o.GetLog().Take(5).ToList() });

            return Ok(new
            {
                Event = evt,
                ObserverNotifications = observerLogs
            });
        }

        [HttpGet("observer/logs")]
        public IActionResult GetObserverLogs()
        {
            var observers = FlightStatusEventBus.Shared.GetObservers()
                .Select(o => new
                {
                    o.ObserverId,
                    o.ObserverType,
                    RecentLog = o.GetLog().Take(10).ToList()
                });

            return Ok(new
            {
                EventHistory = FlightStatusEventBus.Shared.GetHistory().Take(20),
                Observers = observers
            });
        }

        [HttpGet("observer/statuses")]
        public IActionResult GetFlightStatuses() =>
            Ok(Enum.GetNames<FlightStatus>());

        // ── COMMAND ──────────────────────────────────────────────────────

        [HttpPost("command/create-flight")]
        public async Task<IActionResult> CommandCreateFlight([FromBody] CommandCreateFlightRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.FlightNumber))
                return BadRequest("FlightNumber is required.");
            if (req.AirportId <= 0)
                return BadRequest("AirportId must be positive.");

            var cmd = new CreateFlightCommand(_uow, req.FlightNumber, req.AirportId);
            var result = await _commandInvoker.ExecuteAsync(cmd);
            return result.Success ? Ok(new { result, CanUndo = _commandInvoker.CanUndo, NextUndo = _commandInvoker.PeekNextUndo })
                                  : Conflict(result);
        }

        [HttpPost("command/delete-flight/{flightId:int}")]
        public async Task<IActionResult> CommandDeleteFlight(int flightId)
        {
            var cmd = new DeleteFlightCommand(_uow, flightId);
            var result = await _commandInvoker.ExecuteAsync(cmd);
            return result.Success ? Ok(new { result, CanUndo = _commandInvoker.CanUndo, NextUndo = _commandInvoker.PeekNextUndo })
                                  : NotFound(result);
        }

        [HttpPost("command/rename-flight")]
        public async Task<IActionResult> CommandRenameFlight([FromBody] CommandRenameFlightRequest req)
        {
            if (req.FlightId <= 0 || string.IsNullOrWhiteSpace(req.NewFlightNumber))
                return BadRequest("FlightId and NewFlightNumber are required.");

            var cmd = new UpdateFlightNumberCommand(_uow, req.FlightId, req.NewFlightNumber);
            var result = await _commandInvoker.ExecuteAsync(cmd);
            return result.Success ? Ok(new { result, CanUndo = _commandInvoker.CanUndo, NextUndo = _commandInvoker.PeekNextUndo })
                                  : NotFound(result);
        }

        [HttpPost("command/undo")]
        public async Task<IActionResult> UndoLastCommand()
        {
            var result = await _commandInvoker.UndoLastAsync(_uow);
            return result.Success ? Ok(new { result, CanUndo = _commandInvoker.CanUndo, NextUndo = _commandInvoker.PeekNextUndo })
                                  : BadRequest(result);
        }

        [HttpGet("command/history")]
        public IActionResult GetCommandHistory() => Ok(new
        {
            CanUndo = _commandInvoker.CanUndo,
            NextUndo = _commandInvoker.PeekNextUndo,
            Log = _commandInvoker.GetLog()
        });

        // ── MEMENTO ──────────────────────────────────────────────────────

        [HttpGet("memento/config")]
        public IActionResult GetCurrentConfig() =>
            Ok(_caretaker.GetCurrentConfig());

        [HttpPatch("memento/config")]
        public IActionResult UpdateConfig([FromBody] UpdateAirportConfigRequest req)
        {
            _caretaker.ApplyChanges(
                req.TerminalLayout,
                req.ActiveRunways,
                req.MaxDailyFlights,
                req.IsInternationalEnabled,
                req.ActiveGates,
                req.SecurityLevel,
                req.Notes);

            return Ok(_caretaker.GetCurrentConfig());
        }

        [HttpPost("memento/snapshot")]
        public IActionResult SaveSnapshot([FromBody] SaveSnapshotRequest req)
        {
            var id = _caretaker.SaveSnapshot(req.Label ?? "Manual snapshot");
            return Ok(new { MementoId = id, Message = $"Snapshot saved: {req.Label}" });
        }

        [HttpPost("memento/restore/{mementoId}")]
        public IActionResult RestoreSnapshot(string mementoId)
        {
            if (!_caretaker.RestoreSnapshot(mementoId))
                return NotFound($"Snapshot {mementoId} not found.");
            return Ok(new { Message = $"Restored to snapshot {mementoId}", Config = _caretaker.GetCurrentConfig() });
        }

        [HttpPost("memento/undo")]
        public IActionResult UndoConfig()
        {
            if (!_caretaker.RestoreLast())
                return BadRequest("No previous snapshot to restore.");
            return Ok(new { Message = "Reverted to previous configuration", Config = _caretaker.GetCurrentConfig() });
        }

        [HttpGet("memento/history")]
        public IActionResult GetConfigHistory() =>
            Ok(_caretaker.GetHistory());

        // ── ITERATOR ─────────────────────────────────────────────────────

        [HttpGet("iterator/flights")]
        public async Task<IActionResult> IterateFlights(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5,
            [FromQuery] string? airportCode = null)
        {
            var flights = await _uow.Flights.GetAllAsync();
            var airports = await _uow.Airports.GetAllAsync();

            IAirportIterator<FlightIteratorItem> iterator = airportCode != null
                ? AirportIteratorFactory.CreateAirportFlightIterator(flights, airports, airportCode)
                : AirportIteratorFactory.CreateFlightIterator(flights, airports);

            string filterDesc = airportCode != null
                ? $"Flights at airport {airportCode.ToUpper()}"
                : "All flights";

            var result = AirportIteratorFactory.Paginate(iterator, page, pageSize);

            return Ok(new
            {
                IteratorType = airportCode != null ? "FilteredFlightIterator" : "FlightIterator",
                FilterDescription = filterDesc,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages,
                result.Items
            });
        }

        [HttpGet("iterator/facilities")]
        public IActionResult IterateFacilities(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5,
            [FromQuery] string? type = null)
        {
            var iterator = AirportIteratorFactory.CreateFacilityIterator(type);
            var result = AirportIteratorFactory.Paginate(iterator, page, pageSize);

            return Ok(new
            {
                IteratorType = "FacilityIterator",
                FilterDescription = type != null ? $"Facilities of type: {type}" : "All facilities",
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages,
                result.Items
            });
        }

        [HttpGet("iterator/facility-types")]
        public IActionResult GetFacilityTypes() =>
            Ok(new[] { "Gate", "Runway", "Terminal", "Lounge" });
    }

    // ── Request DTOs ──────────────────────────────────────────────────────

    public sealed record ChangeFlightStatusRequest(
        string FlightNumber,
        string NewStatus,
        string? OldStatus = null,
        string? GateCode = null,
        string? Reason = null);

    public sealed record CommandCreateFlightRequest(
        string FlightNumber,
        int AirportId);

    public sealed record CommandRenameFlightRequest(
        int FlightId,
        string NewFlightNumber);

    public sealed class UpdateAirportConfigRequest
    {
        public string? TerminalLayout { get; set; }
        public int? ActiveRunways { get; set; }
        public int? MaxDailyFlights { get; set; }
        public bool? IsInternationalEnabled { get; set; }
        public List<string>? ActiveGates { get; set; }
        public string? SecurityLevel { get; set; }
        public string? Notes { get; set; }
    }

    public sealed record SaveSnapshotRequest(string? Label);
}
