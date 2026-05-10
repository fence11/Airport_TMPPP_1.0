using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Command
{
    // ── Result & log models ───────────────────────────────────────────────────

    public sealed record CommandResult(
        bool    Success,
        string  Message,
        object? Payload = null);

    public sealed record CommandLogEntry(
        string   CommandId,
        string   CommandName,
        string   Description,
        DateTime IssuedAtUtc,
        bool     Success,
        string   Message,
        bool     IsUndo);

    // ── Command interface — IUnitOfWork injected at call time ─────────────────
    //
    // DESIGN DECISION: IUnitOfWork is NOT captured in the constructor.
    // Commands are stored in the static invoker history across HTTP requests.
    // IUnitOfWork is scoped (disposed at request end), so capturing it would
    // cause ObjectDisposedException on undo. Passing it at execute/undo time
    // ensures the current request's live context is always used.

    public interface IFlightCommand
    {
        string   CommandId   { get; }
        string   CommandName { get; }
        string   Description { get; }
        DateTime IssuedAtUtc { get; }
        bool     CanUndo     { get; }

        Task<CommandResult> ExecuteAsync(IUnitOfWork uow);
        Task<CommandResult> UndoAsync   (IUnitOfWork uow);
    }

    // ── Concrete commands ─────────────────────────────────────────────────────

    /// Creates a new flight row in the database.
    /// Undo deletes the row that was created.
    public sealed class CreateFlightCommand : IFlightCommand
    {
        private readonly string _flightNumber;
        private readonly int    _airportId;

        // Captured after a successful Execute so Undo knows what to delete.
        private int _createdFlightId;

        public string   CommandId   { get; } = Guid.NewGuid().ToString("N")[..8];
        public string   CommandName => "CreateFlight";
        public string   Description => $"Create flight {_flightNumber} at airport #{_airportId}";
        public DateTime IssuedAtUtc { get; } = DateTime.UtcNow;
        public bool     CanUndo     => true;

        public CreateFlightCommand(string flightNumber, int airportId)
        {
            _flightNumber = flightNumber;
            _airportId    = airportId;
        }

        public async Task<CommandResult> ExecuteAsync(IUnitOfWork uow)
        {
            var airport = await uow.Airports.GetByIdAsync(_airportId);
            if (airport is null)
                return new CommandResult(false, $"Airport #{_airportId} not found.");

            var flight  = new Flight(_flightNumber, _airportId);
            var created = await uow.Flights.AddAsync(flight);
            _createdFlightId = created.Id;

            return new CommandResult(
                true,
                $"Flight {_flightNumber} created with ID {_createdFlightId}.",
                new { created.Id, created.FlightNumber, created.AirportId });
        }

        public async Task<CommandResult> UndoAsync(IUnitOfWork uow)
        {
            if (_createdFlightId == 0)
                return new CommandResult(false, "Nothing to undo — command was not executed.");

            // Fetch fresh from DB using the current (non-disposed) scoped context.
            var existing = await uow.Flights.GetByIdAsync(_createdFlightId);
            if (existing is null)
                return new CommandResult(false, $"Undo skipped: flight ID {_createdFlightId} already absent.");

            var deleted = await uow.Flights.DeleteAsync(_createdFlightId);
            return deleted
                ? new CommandResult(true,  $"Undo: flight {_flightNumber} (ID {_createdFlightId}) deleted.")
                : new CommandResult(false, $"Undo failed: could not delete flight ID {_createdFlightId}.");
        }
    }

    /// Deletes a flight row from the database.
    /// Undo re-inserts it with its original flight number and airport.
    public sealed class DeleteFlightCommand : IFlightCommand
    {
        private readonly int _flightId;

        // Snapshot captured during Execute so Undo can rebuild the row.
        private string? _backupFlightNumber;
        private int?    _backupAirportId;

        public string   CommandId   { get; } = Guid.NewGuid().ToString("N")[..8];
        public string   CommandName => "DeleteFlight";
        public string   Description => $"Delete flight ID #{_flightId}";
        public DateTime IssuedAtUtc { get; } = DateTime.UtcNow;
        public bool     CanUndo     => true;

        public DeleteFlightCommand(int flightId) => _flightId = flightId;

        public async Task<CommandResult> ExecuteAsync(IUnitOfWork uow)
        {
            var flight = await uow.Flights.GetByIdAsync(_flightId);
            if (flight is null)
                return new CommandResult(false, $"Flight #{_flightId} not found.");

            // Snapshot the values we need for undo BEFORE deletion.
            _backupFlightNumber = flight.FlightNumber;
            _backupAirportId    = flight.AirportId;

            var deleted = await uow.Flights.DeleteAsync(_flightId);
            return deleted
                ? new CommandResult(true,  $"Flight {_backupFlightNumber} (ID {_flightId}) deleted.")
                : new CommandResult(false, $"Delete failed for flight #{_flightId}.");
        }

        public async Task<CommandResult> UndoAsync(IUnitOfWork uow)
        {
            if (_backupFlightNumber is null || _backupAirportId is null)
                return new CommandResult(false, "Nothing to undo — Execute was never called or failed.");

            var restored = await uow.Flights.AddAsync(new Flight(_backupFlightNumber, _backupAirportId.Value));
            return new CommandResult(
                true,
                $"Undo: flight {_backupFlightNumber} restored as ID {restored.Id}.",
                new { restored.Id, restored.FlightNumber });
        }
    }

    /// Renames a flight (changes FlightNumber).
    /// Undo restores the original flight number.
    public sealed class UpdateFlightNumberCommand : IFlightCommand
    {
        private readonly int    _flightId;
        private readonly string _newFlightNumber;

        // Captured during Execute.
        private string? _oldFlightNumber;
        private int?    _airportId;

        public string   CommandId   { get; } = Guid.NewGuid().ToString("N")[..8];
        public string   CommandName => "UpdateFlightNumber";
        public string   Description => $"Rename flight #{_flightId} to {_newFlightNumber}";
        public DateTime IssuedAtUtc { get; } = DateTime.UtcNow;
        public bool     CanUndo     => true;

        public UpdateFlightNumberCommand(int flightId, string newFlightNumber)
        {
            _flightId        = flightId;
            _newFlightNumber = newFlightNumber;
        }

        public async Task<CommandResult> ExecuteAsync(IUnitOfWork uow)
        {
            // Load the TRACKED entity — we mutate it in place so EF Core
            // has a single tracked instance and no identity-conflict can occur.
            var flight = await uow.Flights.GetByIdAsync(_flightId);
            if (flight is null)
                return new CommandResult(false, $"Flight #{_flightId} not found.");

            _oldFlightNumber = flight.FlightNumber;
            _airportId       = flight.AirportId;

            // Use reflection to set the private-setter property (matches existing pattern).
            // Alternatively the Flight entity could expose an UpdateFlightNumber() method.
            flight.GetType().GetProperty("FlightNumber")?.SetValue(flight, _newFlightNumber);
            flight.MarkUpdated();

            // Pass the SAME tracked instance — no new object, no conflict.
            await uow.Flights.UpdateAsync(flight);

            return new CommandResult(
                true,
                $"Flight #{_flightId} renamed {_oldFlightNumber} → {_newFlightNumber}.",
                new { Id = _flightId, OldNumber = _oldFlightNumber, NewNumber = _newFlightNumber });
        }

        public async Task<CommandResult> UndoAsync(IUnitOfWork uow)
        {
            if (_oldFlightNumber is null || _airportId is null)
                return new CommandResult(false, "Nothing to undo — Execute was never called or failed.");

            // Again: load the tracked entity, mutate it, update — never create a new object.
            var flight = await uow.Flights.GetByIdAsync(_flightId);
            if (flight is null)
                return new CommandResult(false, $"Undo failed: flight #{_flightId} not found.");

            flight.GetType().GetProperty("FlightNumber")?.SetValue(flight, _oldFlightNumber);
            flight.MarkUpdated();
            await uow.Flights.UpdateAsync(flight);

            return new CommandResult(
                true,
                $"Undo: flight #{_flightId} reverted to {_oldFlightNumber}.");
        }
    }

    // ── Invoker — stores commands, IUnitOfWork passed per call ───────────────
    //
    // The invoker lives as a static field on the controller (shared across
    // requests). It stores IFlightCommand instances but NEVER stores
    // IUnitOfWork. The controller passes its current scoped IUnitOfWork
    // into ExecuteAsync / UndoLastAsync on every HTTP request.

    public sealed class FlightCommandInvoker
    {
        private readonly Stack<IFlightCommand>                                     _history = new();
        private readonly List<(IFlightCommand Cmd, CommandResult Res, bool IsUndo)> _log    = new();

        public async Task<CommandResult> ExecuteAsync(IFlightCommand command, IUnitOfWork uow)
        {
            var result = await command.ExecuteAsync(uow);

            if (result.Success && command.CanUndo)
                _history.Push(command);

            _log.Insert(0, (command, result, false));
            if (_log.Count > 50) _log.RemoveAt(_log.Count - 1);

            return result;
        }

        public async Task<CommandResult> UndoLastAsync(IUnitOfWork uow)
        {
            if (!_history.TryPop(out var command))
                return new CommandResult(false, "Nothing to undo.");

            // IUnitOfWork from the current (live) HTTP request scope is used here.
            var result = await command.UndoAsync(uow);

            _log.Insert(0, (command, result, true));
            if (_log.Count > 50) _log.RemoveAt(_log.Count - 1);

            return result;
        }

        public bool    CanUndo       => _history.Count > 0;
        public string? PeekNextUndo  => _history.TryPeek(out var c) ? c.Description : null;

        public IReadOnlyList<CommandLogEntry> GetLog() =>
            _log.Select(e => new CommandLogEntry(
                e.Cmd.CommandId,
                e.Cmd.CommandName,
                e.Cmd.Description,
                e.Cmd.IssuedAtUtc,
                e.Res.Success,
                e.Res.Message,
                e.IsUndo))
            .ToList()
            .AsReadOnly();
    }
}
