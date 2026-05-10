using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Command
{
    public interface IFlightCommand
    {
        string CommandId { get; }
        string CommandName { get; }
        string Description { get; }
        DateTime IssuedAtUtc { get; }
        bool CanUndo { get; }
        Task<CommandResult> ExecuteAsync();
        Task<CommandResult> UndoAsync(IUnitOfWork uow);
    }

    public sealed record CommandResult(
        bool Success,
        string Message,
        object? Payload = null);

    // ── Concrete Commands ──────────────────────────────────────────────────

    public sealed class CreateFlightCommand : IFlightCommand
    {
        private readonly IUnitOfWork _uow;
        private readonly string _flightNumber;
        private readonly int _airportId;
        private int _createdFlightId;

        public string CommandId { get; } = Guid.NewGuid().ToString("N")[..8];
        public string CommandName => "CreateFlight";
        public string Description => $"Create flight {_flightNumber} at airport #{_airportId}";
        public DateTime IssuedAtUtc { get; } = DateTime.UtcNow;
        public bool CanUndo => true;

        public CreateFlightCommand(IUnitOfWork uow, string flightNumber, int airportId)
        {
            _uow = uow;
            _flightNumber = flightNumber;
            _airportId = airportId;
        }

        public async Task<CommandResult> ExecuteAsync()
        {
            var airport = await _uow.Airports.GetByIdAsync(_airportId);
            if (airport is null)
                return new CommandResult(false, $"Airport #{_airportId} not found.");

            var flight = new Flight(_flightNumber, _airportId);
            var created = await _uow.Flights.AddAsync(flight);
            _createdFlightId = created.Id;

            return new CommandResult(true,
                $"Flight {_flightNumber} created with ID {_createdFlightId}.",
                new { created.Id, created.FlightNumber, created.AirportId });
        }

        public async Task<CommandResult> UndoAsync(IUnitOfWork uow)
        {
            if (_createdFlightId == 0)
                return new CommandResult(false, "Nothing to undo — command was not executed.");

            var deleted = await uow.Flights.DeleteAsync(_createdFlightId);
            return deleted
                ? new CommandResult(true, $"Undo: flight {_flightNumber} (ID {_createdFlightId}) deleted.")
                : new CommandResult(false, $"Undo failed: flight ID {_createdFlightId} not found.");
        }
    }

    public sealed class DeleteFlightCommand : IFlightCommand
    {
        private readonly IUnitOfWork _uow;
        private readonly int _flightId;
        private Flight? _backup;

        public string CommandId { get; } = Guid.NewGuid().ToString("N")[..8];
        public string CommandName => "DeleteFlight";
        public string Description => $"Delete flight ID #{_flightId}";
        public DateTime IssuedAtUtc { get; } = DateTime.UtcNow;
        public bool CanUndo => true;

        public DeleteFlightCommand(IUnitOfWork uow, int flightId)
        {
            _uow = uow;
            _flightId = flightId;
        }

        public async Task<CommandResult> ExecuteAsync()
        {
            _backup = await _uow.Flights.GetByIdAsync(_flightId);
            if (_backup is null)
                return new CommandResult(false, $"Flight #{_flightId} not found.");

            var deleted = await _uow.Flights.DeleteAsync(_flightId);
            return deleted
                ? new CommandResult(true, $"Flight {_backup.FlightNumber} (ID {_flightId}) deleted.")
                : new CommandResult(false, $"Delete failed for flight #{_flightId}.");
        }

        public async Task<CommandResult> UndoAsync(IUnitOfWork uow)
        {
            if (_backup is null)
                return new CommandResult(false, "Nothing to undo.");

            var restored = await uow.Flights.AddAsync(new Flight(_backup.FlightNumber, _backup.AirportId));
            return new CommandResult(true,
                $"Undo: flight {_backup.FlightNumber} restored as ID {restored.Id}.",
                new { restored.Id, restored.FlightNumber });
        }
    }

    public sealed class UpdateFlightNumberCommand : IFlightCommand
    {
        private readonly IUnitOfWork _uow;
        private readonly int _flightId;
        private readonly string _newFlightNumber;
        private string? _oldFlightNumber;
        private int? _airportId;

        public string CommandId { get; } = Guid.NewGuid().ToString("N")[..8];
        public string CommandName => "UpdateFlightNumber";
        public string Description => $"Rename flight #{_flightId} to {_newFlightNumber}";
        public DateTime IssuedAtUtc { get; } = DateTime.UtcNow;
        public bool CanUndo => true;

        public UpdateFlightNumberCommand(IUnitOfWork uow, int flightId, string newFlightNumber)
        {
            _uow = uow;
            _flightId = flightId;
            _newFlightNumber = newFlightNumber;
        }

        public async Task<CommandResult> ExecuteAsync()
        {
            var flight = await _uow.Flights.GetByIdAsync(_flightId);
            if (flight is null)
                return new CommandResult(false, $"Flight #{_flightId} not found.");

            _oldFlightNumber = flight.FlightNumber;
            _airportId = flight.AirportId;

            var updated = new Flight(_newFlightNumber, flight.AirportId);
            updated.GetType().GetProperty("Id")?.SetValue(updated, _flightId);
            await _uow.Flights.UpdateAsync(updated);

            return new CommandResult(true,
                $"Flight #{_flightId} renamed from {_oldFlightNumber} to {_newFlightNumber}.",
                new { Id = _flightId, OldNumber = _oldFlightNumber, NewNumber = _newFlightNumber });
        }

        public async Task<CommandResult> UndoAsync(IUnitOfWork uow)
        {
            if (_oldFlightNumber is null || _airportId is null)
                return new CommandResult(false, "Nothing to undo.");

            var reverted = new Flight(_oldFlightNumber, _airportId.Value);
            reverted.GetType().GetProperty("Id")?.SetValue(reverted, _flightId);
            await uow.Flights.UpdateAsync(reverted);

            return new CommandResult(true,
                $"Undo: flight #{_flightId} reverted to {_oldFlightNumber}.");
        }
    }

    // ── Command Invoker / History ──────────────────────────────────────────

    public sealed class FlightCommandInvoker
    {
        private readonly Stack<IFlightCommand> _history = new();
        private readonly List<(IFlightCommand Command, CommandResult Result, bool IsUndo)> _log = new();

        public async Task<CommandResult> ExecuteAsync(IFlightCommand command)
        {
            var result = await command.ExecuteAsync();
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

            var result = await command.UndoAsync(uow);
            _log.Insert(0, (command, result, true));
            return result;
        }

        public bool CanUndo => _history.Count > 0;
        public string? PeekNextUndo => _history.TryPeek(out var c) ? c.Description : null;

        public IReadOnlyList<CommandLogEntry> GetLog() =>
            _log.Select(e => new CommandLogEntry(
                e.Command.CommandId,
                e.Command.CommandName,
                e.Command.Description,
                e.Command.IssuedAtUtc,
                e.Result.Success,
                e.Result.Message,
                e.IsUndo)).ToList().AsReadOnly();
    }

    public sealed record CommandLogEntry(
        string CommandId,
        string CommandName,
        string Description,
        DateTime IssuedAtUtc,
        bool Success,
        string Message,
        bool IsUndo);
}
