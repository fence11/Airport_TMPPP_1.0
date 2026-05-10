namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Observer
{
    public enum FlightStatus
    {
        Scheduled,
        Boarding,
        Departed,
        InAir,
        Landed,
        Delayed,
        Cancelled
    }

    public sealed record FlightStatusChangedEvent(
        int FlightId,
        string FlightNumber,
        FlightStatus OldStatus,
        FlightStatus NewStatus,
        string? GateCode,
        string? Reason,
        DateTime ChangedAtUtc);

    public interface IFlightStatusObserver
    {
        string ObserverId { get; }
        string ObserverType { get; }
        void OnStatusChanged(FlightStatusChangedEvent evt);
        IReadOnlyList<string> GetLog();
    }

    public interface IFlightStatusSubject
    {
        void Subscribe(IFlightStatusObserver observer);
        void Unsubscribe(string observerId);
        void NotifyAll(FlightStatusChangedEvent evt);
    }

    // Concrete observers
    public sealed class GateDisplayObserver : IFlightStatusObserver
    {
        private readonly List<string> _log = new();
        public string ObserverId => "GateDisplay";
        public string ObserverType => "Gate Display Board";

        public void OnStatusChanged(FlightStatusChangedEvent evt)
        {
            var entry = evt.NewStatus switch
            {
                FlightStatus.Boarding  => $"[GATE {evt.GateCode ?? "TBD"}] ✈ {evt.FlightNumber} — NOW BOARDING",
                FlightStatus.Departed  => $"[GATE {evt.GateCode ?? "TBD"}] ✈ {evt.FlightNumber} — DEPARTED",
                FlightStatus.Delayed   => $"[GATE {evt.GateCode ?? "TBD"}] ⚠ {evt.FlightNumber} — DELAYED: {evt.Reason}",
                FlightStatus.Cancelled => $"[GATE {evt.GateCode ?? "TBD"}] ✗ {evt.FlightNumber} — CANCELLED",
                _ => $"[GATE {evt.GateCode ?? "TBD"}] {evt.FlightNumber} — {evt.NewStatus}"
            };
            _log.Insert(0, $"{evt.ChangedAtUtc:HH:mm:ss} {entry}");
            if (_log.Count > 50) _log.RemoveAt(_log.Count - 1);
        }

        public IReadOnlyList<string> GetLog() => _log.AsReadOnly();
    }

    public sealed class PassengerNotificationObserver : IFlightStatusObserver
    {
        private readonly List<string> _log = new();
        public string ObserverId => "PassengerNotify";
        public string ObserverType => "Passenger SMS/Email Service";

        public void OnStatusChanged(FlightStatusChangedEvent evt)
        {
            var msg = evt.NewStatus switch
            {
                FlightStatus.Boarding  => $"📱 SMS sent: Your flight {evt.FlightNumber} is boarding at gate {evt.GateCode ?? "TBD"}. Please proceed now.",
                FlightStatus.Delayed   => $"📧 Email sent: Flight {evt.FlightNumber} delayed. Reason: {evt.Reason ?? "operational"}.",
                FlightStatus.Cancelled => $"📧 Email+SMS: Flight {evt.FlightNumber} CANCELLED. Rebooking link sent.",
                FlightStatus.Departed  => $"📱 SMS sent: Flight {evt.FlightNumber} has departed. Safe travels!",
                _ => $"📱 Notification: Flight {evt.FlightNumber} status → {evt.NewStatus}"
            };
            _log.Insert(0, $"{evt.ChangedAtUtc:HH:mm:ss} {msg}");
            if (_log.Count > 50) _log.RemoveAt(_log.Count - 1);
        }

        public IReadOnlyList<string> GetLog() => _log.AsReadOnly();
    }

    public sealed class AirTrafficControlObserver : IFlightStatusObserver
    {
        private readonly List<string> _log = new();
        public string ObserverId => "ATC";
        public string ObserverType => "Air Traffic Control";

        public void OnStatusChanged(FlightStatusChangedEvent evt)
        {
            var entry = $"[ATC] Flight {evt.FlightNumber}: {evt.OldStatus} → {evt.NewStatus} at {evt.ChangedAtUtc:HH:mm:ss} UTC";
            if (evt.Reason != null) _ = entry + $" | Note: {evt.Reason}";
            _log.Insert(0, $"{evt.ChangedAtUtc:HH:mm:ss} [ATC-LOG] {evt.FlightNumber} {evt.OldStatus}→{evt.NewStatus}{(evt.Reason != null ? " | " + evt.Reason : "")}");
            if (_log.Count > 50) _log.RemoveAt(_log.Count - 1);
        }

        public IReadOnlyList<string> GetLog() => _log.AsReadOnly();
    }

    public sealed class BaggageSystemObserver : IFlightStatusObserver
    {
        private readonly List<string> _log = new();
        public string ObserverId => "Baggage";
        public string ObserverType => "Baggage Handling System";

        public void OnStatusChanged(FlightStatusChangedEvent evt)
        {
            if (evt.NewStatus is FlightStatus.Boarding or FlightStatus.Departed or FlightStatus.Cancelled)
            {
                var action = evt.NewStatus switch
                {
                    FlightStatus.Boarding  => $"🧳 Baggage carousel activated for flight {evt.FlightNumber}",
                    FlightStatus.Departed  => $"🧳 Baggage loading confirmed for flight {evt.FlightNumber}",
                    FlightStatus.Cancelled => $"🧳 Baggage retrieval initiated for {evt.FlightNumber} — all bags to reclaim belt 3",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(action))
                {
                    _log.Insert(0, $"{evt.ChangedAtUtc:HH:mm:ss} {action}");
                    if (_log.Count > 50) _log.RemoveAt(_log.Count - 1);
                }
            }
        }

        public IReadOnlyList<string> GetLog() => _log.AsReadOnly();
    }

    // Subject / Event Bus
    public sealed class FlightStatusEventBus : IFlightStatusSubject
    {
        private readonly Dictionary<string, IFlightStatusObserver> _observers = new();
        private readonly List<FlightStatusChangedEvent> _eventHistory = new();

        // Singleton-style shared instance for demo (scoped in DI)
        private static readonly Lazy<FlightStatusEventBus> _shared =
            new(() =>
            {
                var bus = new FlightStatusEventBus();
                bus.Subscribe(new GateDisplayObserver());
                bus.Subscribe(new PassengerNotificationObserver());
                bus.Subscribe(new AirTrafficControlObserver());
                bus.Subscribe(new BaggageSystemObserver());
                return bus;
            });

        public static FlightStatusEventBus Shared => _shared.Value;

        public void Subscribe(IFlightStatusObserver observer) =>
            _observers[observer.ObserverId] = observer;

        public void Unsubscribe(string observerId) =>
            _observers.Remove(observerId);

        public void NotifyAll(FlightStatusChangedEvent evt)
        {
            _eventHistory.Insert(0, evt);
            if (_eventHistory.Count > 100) _eventHistory.RemoveAt(_eventHistory.Count - 1);
            foreach (var obs in _observers.Values)
                obs.OnStatusChanged(evt);
        }

        public IReadOnlyList<FlightStatusChangedEvent> GetHistory() => _eventHistory.AsReadOnly();

        public IReadOnlyList<IFlightStatusObserver> GetObservers() =>
            _observers.Values.ToList().AsReadOnly();
    }
}
