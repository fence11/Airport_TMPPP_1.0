using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.State
{
    // ── State enum (stored in DB as string) ───────────────────────────────────

    public enum FlightLifecycleStatus
    {
        Scheduled,
        CheckInOpen,
        Boarding,
        Departed,
        InAir,
        Landed,
        Delayed,
        Cancelled
    }

    // ── Context ───────────────────────────────────────────────────────────────

    public sealed class FlightContext
    {
        public int    FlightId     { get; }
        public string FlightNumber { get; }

        private IFlightState _state;

        public FlightLifecycleStatus CurrentStatus => _state.Status;
        public string                StatusLabel   => _state.Label;
        public string                StatusDescription => _state.Description;
        public IReadOnlyList<string> AllowedTransitions => _state.AllowedTransitions;

        // Audit trail (in-memory per context instance)
        private readonly List<StateTransitionRecord> _history = new();
        public IReadOnlyList<StateTransitionRecord> History => _history.AsReadOnly();

        public FlightContext(int flightId, string flightNumber, FlightLifecycleStatus initial = FlightLifecycleStatus.Scheduled)
        {
            FlightId     = flightId;
            FlightNumber = flightNumber;
            _state       = StateFactory.Create(initial, this);
        }

        internal void TransitionTo(IFlightState newState, string actor, string? note)
        {
            var record = new StateTransitionRecord(
                FlightId,
                FlightNumber,
                _state.Status,
                newState.Status,
                actor,
                note,
                DateTime.UtcNow);

            _state = newState;
            _history.Insert(0, record);
        }

        // ── Actions delegated to current state ────────────────────────────────

        public StateActionResult OpenCheckIn(string actor, string? note = null) =>
            _state.OpenCheckIn(actor, note);

        public StateActionResult StartBoarding(string actor, string? note = null) =>
            _state.StartBoarding(actor, note);

        public StateActionResult Depart(string actor, string? note = null) =>
            _state.Depart(actor, note);

        public StateActionResult MarkInAir(string actor, string? note = null) =>
            _state.MarkInAir(actor, note);

        public StateActionResult Land(string actor, string? note = null) =>
            _state.Land(actor, note);

        public StateActionResult Delay(string actor, string reason) =>
            _state.Delay(actor, reason);

        public StateActionResult Cancel(string actor, string reason) =>
            _state.Cancel(actor, reason);

        public StateActionResult Reschedule(string actor, string? note = null) =>
            _state.Reschedule(actor, note);
    }

    public sealed record StateActionResult(bool Success, string Message, FlightLifecycleStatus? NewStatus);
    public sealed record StateTransitionRecord(
        int    FlightId,
        string FlightNumber,
        FlightLifecycleStatus From,
        FlightLifecycleStatus To,
        string Actor,
        string? Note,
        DateTime OccurredAtUtc);

    // ── State interface ───────────────────────────────────────────────────────

    public interface IFlightState
    {
        FlightLifecycleStatus    Status             { get; }
        string                   Label              { get; }
        string                   Description        { get; }
        IReadOnlyList<string>    AllowedTransitions { get; }

        StateActionResult OpenCheckIn (string actor, string? note);
        StateActionResult StartBoarding(string actor, string? note);
        StateActionResult Depart       (string actor, string? note);
        StateActionResult MarkInAir    (string actor, string? note);
        StateActionResult Land         (string actor, string? note);
        StateActionResult Delay        (string actor, string reason);
        StateActionResult Cancel       (string actor, string reason);
        StateActionResult Reschedule   (string actor, string? note);
    }

    // ── Base state (default: reject all with helpful message) ─────────────────

    public abstract class FlightStateBase : IFlightState
    {
        protected readonly FlightContext _ctx;
        protected FlightStateBase(FlightContext ctx) => _ctx = ctx;

        public abstract FlightLifecycleStatus  Status             { get; }
        public abstract string                 Label              { get; }
        public abstract string                 Description        { get; }
        public abstract IReadOnlyList<string>  AllowedTransitions { get; }

        protected StateActionResult Invalid(string action) =>
            new(false, $"Cannot '{action}' when flight is '{Label}'. Allowed: [{string.Join(", ", AllowedTransitions)}].", null);

        protected StateActionResult Transition(IFlightState next, string actor, string? note)
        {
            _ctx.TransitionTo(next, actor, note);
            return new(true, $"Flight {_ctx.FlightNumber}: {Label} → {next.Label}.", next.Status);
        }

        public virtual StateActionResult OpenCheckIn (string a, string? n) => Invalid("OpenCheckIn");
        public virtual StateActionResult StartBoarding(string a, string? n) => Invalid("StartBoarding");
        public virtual StateActionResult Depart       (string a, string? n) => Invalid("Depart");
        public virtual StateActionResult MarkInAir    (string a, string? n) => Invalid("MarkInAir");
        public virtual StateActionResult Land         (string a, string? n) => Invalid("Land");
        public virtual StateActionResult Delay        (string a, string  r) => Invalid("Delay");
        public virtual StateActionResult Cancel       (string a, string  r) => Invalid("Cancel");
        public virtual StateActionResult Reschedule   (string a, string? n) => Invalid("Reschedule");
    }

    // ── Concrete states ───────────────────────────────────────────────────────

    public sealed class ScheduledState : FlightStateBase
    {
        public ScheduledState(FlightContext ctx) : base(ctx) { }
        public override FlightLifecycleStatus Status => FlightLifecycleStatus.Scheduled;
        public override string Label => "Scheduled";
        public override string Description => "Flight is confirmed and on the timetable.";
        public override IReadOnlyList<string> AllowedTransitions => new[] { "OpenCheckIn", "Delay", "Cancel" };

        public override StateActionResult OpenCheckIn(string a, string? n) =>
            Transition(new CheckInOpenState(_ctx), a, n);
        public override StateActionResult Delay(string a, string r) =>
            Transition(new DelayedState(_ctx), a, r);
        public override StateActionResult Cancel(string a, string r) =>
            Transition(new CancelledState(_ctx), a, r);
    }

    public sealed class CheckInOpenState : FlightStateBase
    {
        public CheckInOpenState(FlightContext ctx) : base(ctx) { }
        public override FlightLifecycleStatus Status => FlightLifecycleStatus.CheckInOpen;
        public override string Label => "Check-In Open";
        public override string Description => "Passengers may check in and drop baggage.";
        public override IReadOnlyList<string> AllowedTransitions => new[] { "StartBoarding", "Delay", "Cancel" };

        public override StateActionResult StartBoarding(string a, string? n) =>
            Transition(new BoardingState(_ctx), a, n);
        public override StateActionResult Delay(string a, string r) =>
            Transition(new DelayedState(_ctx), a, r);
        public override StateActionResult Cancel(string a, string r) =>
            Transition(new CancelledState(_ctx), a, r);
    }

    public sealed class BoardingState : FlightStateBase
    {
        public BoardingState(FlightContext ctx) : base(ctx) { }
        public override FlightLifecycleStatus Status => FlightLifecycleStatus.Boarding;
        public override string Label => "Boarding";
        public override string Description => "Gate is open; passengers are boarding the aircraft.";
        public override IReadOnlyList<string> AllowedTransitions => new[] { "Depart", "Delay", "Cancel" };

        public override StateActionResult Depart(string a, string? n) =>
            Transition(new DepartedState(_ctx), a, n);
        public override StateActionResult Delay(string a, string r) =>
            Transition(new DelayedState(_ctx), a, r);
        public override StateActionResult Cancel(string a, string r) =>
            Transition(new CancelledState(_ctx), a, r);
    }

    public sealed class DepartedState : FlightStateBase
    {
        public DepartedState(FlightContext ctx) : base(ctx) { }
        public override FlightLifecycleStatus Status => FlightLifecycleStatus.Departed;
        public override string Label => "Departed";
        public override string Description => "Aircraft has left the gate and is taxiing or airborne.";
        public override IReadOnlyList<string> AllowedTransitions => new[] { "MarkInAir" };

        public override StateActionResult MarkInAir(string a, string? n) =>
            Transition(new InAirState(_ctx), a, n);
    }

    public sealed class InAirState : FlightStateBase
    {
        public InAirState(FlightContext ctx) : base(ctx) { }
        public override FlightLifecycleStatus Status => FlightLifecycleStatus.InAir;
        public override string Label => "In Air";
        public override string Description => "Aircraft is in flight en route to destination.";
        public override IReadOnlyList<string> AllowedTransitions => new[] { "Land" };

        public override StateActionResult Land(string a, string? n) =>
            Transition(new LandedState(_ctx), a, n);
    }

    public sealed class LandedState : FlightStateBase
    {
        public LandedState(FlightContext ctx) : base(ctx) { }
        public override FlightLifecycleStatus Status => FlightLifecycleStatus.Landed;
        public override string Label => "Landed";
        public override string Description => "Aircraft has arrived at the destination. Flight complete.";
        public override IReadOnlyList<string> AllowedTransitions => Array.Empty<string>();
    }

    public sealed class DelayedState : FlightStateBase
    {
        public DelayedState(FlightContext ctx) : base(ctx) { }
        public override FlightLifecycleStatus Status => FlightLifecycleStatus.Delayed;
        public override string Label => "Delayed";
        public override string Description => "Flight is delayed; new departure time pending.";
        public override IReadOnlyList<string> AllowedTransitions => new[] { "Reschedule", "Cancel" };

        public override StateActionResult Reschedule(string a, string? n) =>
            Transition(new ScheduledState(_ctx), a, n);
        public override StateActionResult Cancel(string a, string r) =>
            Transition(new CancelledState(_ctx), a, r);
    }

    public sealed class CancelledState : FlightStateBase
    {
        public CancelledState(FlightContext ctx) : base(ctx) { }
        public override FlightLifecycleStatus Status => FlightLifecycleStatus.Cancelled;
        public override string Label => "Cancelled";
        public override string Description => "Flight has been cancelled. No further transitions.";
        public override IReadOnlyList<string> AllowedTransitions => Array.Empty<string>();
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static class StateFactory
    {
        public static IFlightState Create(FlightLifecycleStatus status, FlightContext ctx) => status switch
        {
            FlightLifecycleStatus.Scheduled   => new ScheduledState(ctx),
            FlightLifecycleStatus.CheckInOpen => new CheckInOpenState(ctx),
            FlightLifecycleStatus.Boarding    => new BoardingState(ctx),
            FlightLifecycleStatus.Departed    => new DepartedState(ctx),
            FlightLifecycleStatus.InAir       => new InAirState(ctx),
            FlightLifecycleStatus.Landed      => new LandedState(ctx),
            FlightLifecycleStatus.Delayed     => new DelayedState(ctx),
            FlightLifecycleStatus.Cancelled   => new CancelledState(ctx),
            _ => new ScheduledState(ctx)
        };
    }

    // ── In-memory flight state registry (backed by DB flight records) ─────────

    public sealed class FlightStateRegistry
    {
        private readonly Dictionary<int, FlightContext> _contexts = new();

        public async Task<FlightContext> GetOrCreateAsync(int flightId, IUnitOfWork uow)
        {
            if (_contexts.TryGetValue(flightId, out var existing))
                return existing;

            var flight = await uow.Flights.GetByIdAsync(flightId)
                ?? throw new KeyNotFoundException($"Flight #{flightId} not found.");

            var ctx = new FlightContext(flightId, flight.FlightNumber, FlightLifecycleStatus.Scheduled);
            _contexts[flightId] = ctx;
            return ctx;
        }

        public FlightContext? TryGet(int flightId) =>
            _contexts.TryGetValue(flightId, out var c) ? c : null;

        public IReadOnlyDictionary<int, FlightContext> All => _contexts;
    }
}
