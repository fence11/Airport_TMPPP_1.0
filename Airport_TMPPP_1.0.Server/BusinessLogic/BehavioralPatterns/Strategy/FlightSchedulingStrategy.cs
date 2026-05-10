using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Strategy
{
    public sealed record ScheduledFlight(
        int FlightId,
        string FlightNumber,
        int RunwayId,
        DateTime ScheduledTime,
        string StrategyUsed,
        string Reason);

    public sealed record RunwaySlot(
        int RunwayId,
        string RunwayCode,
        bool IsAvailable,
        DateTime? NextAvailableAt);

    public interface IFlightSchedulingStrategy
    {
        string StrategyName { get; }
        string Description { get; }
        IEnumerable<ScheduledFlight> Schedule(
            IEnumerable<Flight> flights,
            IEnumerable<RunwaySlot> runways,
            DateTime windowStart,
            DateTime windowEnd);
    }

    /// <summary>Schedules flights FCFS — earliest requested time gets the next free runway.</summary>
    public sealed class FirstComeFirstServedStrategy : IFlightSchedulingStrategy
    {
        public string StrategyName => "FCFS";
        public string Description => "First-Come, First-Served: flights are assigned to runways in the order they were registered.";

        public IEnumerable<ScheduledFlight> Schedule(
            IEnumerable<Flight> flights,
            IEnumerable<RunwaySlot> runways,
            DateTime windowStart,
            DateTime windowEnd)
        {
            var orderedFlights = flights.OrderBy(f => f.CreatedAt).ToList();
            var runwayList = runways.Where(r => r.IsAvailable).ToList();
            if (!runwayList.Any()) return Enumerable.Empty<ScheduledFlight>();

            var results = new List<ScheduledFlight>();
            var runwayNextFree = runwayList.ToDictionary(r => r.RunwayId, r => windowStart);
            int runwayIndex = 0;

            foreach (var flight in orderedFlights)
            {
                var runway = runwayList[runwayIndex % runwayList.Count];
                var slot = runwayNextFree[runway.RunwayId];

                results.Add(new ScheduledFlight(
                    flight.Id,
                    flight.FlightNumber,
                    runway.RunwayId,
                    slot,
                    StrategyName,
                    $"Assigned in registration order to runway {runway.RunwayCode}"));

                runwayNextFree[runway.RunwayId] = slot.AddMinutes(15);
                runwayIndex++;
            }

            return results;
        }
    }

    /// <summary>Shortest-flight-number-first heuristic (proxy for shortest turnaround).</summary>
    public sealed class ShortestJobFirstStrategy : IFlightSchedulingStrategy
    {
        public string StrategyName => "SJF";
        public string Description => "Shortest-Job-First: flights with shorter flight numbers (shorter service time estimate) are scheduled first to minimise average wait.";

        public IEnumerable<ScheduledFlight> Schedule(
            IEnumerable<Flight> flights,
            IEnumerable<RunwaySlot> runways,
            DateTime windowStart,
            DateTime windowEnd)
        {
            var orderedFlights = flights.OrderBy(f => f.FlightNumber.Length).ThenBy(f => f.FlightNumber).ToList();
            var runwayList = runways.Where(r => r.IsAvailable).ToList();
            if (!runwayList.Any()) return Enumerable.Empty<ScheduledFlight>();

            var results = new List<ScheduledFlight>();
            var runwayNextFree = runwayList.ToDictionary(r => r.RunwayId, r => windowStart);

            foreach (var flight in orderedFlights)
            {
                // pick runway with earliest free slot
                var runway = runwayList.MinBy(r => runwayNextFree[r.RunwayId])!;
                var slot = runwayNextFree[runway.RunwayId];

                results.Add(new ScheduledFlight(
                    flight.Id,
                    flight.FlightNumber,
                    runway.RunwayId,
                    slot,
                    StrategyName,
                    $"Short-turnaround priority → runway {runway.RunwayCode}"));

                runwayNextFree[runway.RunwayId] = slot.AddMinutes(10);
            }

            return results;
        }
    }

    /// <summary>Round-robin distribution across all available runways for maximum throughput.</summary>
    public sealed class RoundRobinStrategy : IFlightSchedulingStrategy
    {
        public string StrategyName => "RoundRobin";
        public string Description => "Round-Robin: flights are distributed evenly across all available runways to maximise throughput and prevent runway starvation.";

        public IEnumerable<ScheduledFlight> Schedule(
            IEnumerable<Flight> flights,
            IEnumerable<RunwaySlot> runways,
            DateTime windowStart,
            DateTime windowEnd)
        {
            var flightList = flights.ToList();
            var runwayList = runways.Where(r => r.IsAvailable).ToList();
            if (!runwayList.Any()) return Enumerable.Empty<ScheduledFlight>();

            var results = new List<ScheduledFlight>();
            var runwayNextFree = runwayList.ToDictionary(r => r.RunwayId, r => windowStart);
            int idx = 0;

            foreach (var flight in flightList)
            {
                var runway = runwayList[idx % runwayList.Count];
                var slot = runwayNextFree[runway.RunwayId];

                results.Add(new ScheduledFlight(
                    flight.Id,
                    flight.FlightNumber,
                    runway.RunwayId,
                    slot,
                    StrategyName,
                    $"Round-robin turn → runway {runway.RunwayCode}"));

                runwayNextFree[runway.RunwayId] = slot.AddMinutes(12);
                idx++;
            }

            return results;
        }
    }

    public sealed class FlightScheduler
    {
        private IFlightSchedulingStrategy _strategy;

        public FlightScheduler(IFlightSchedulingStrategy strategy) =>
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

        public void SetStrategy(IFlightSchedulingStrategy strategy) =>
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

        public string CurrentStrategy => _strategy.StrategyName;
        public string CurrentDescription => _strategy.Description;

        public IEnumerable<ScheduledFlight> RunSchedule(
            IEnumerable<Flight> flights,
            IEnumerable<RunwaySlot> runways,
            DateTime windowStart,
            DateTime windowEnd) =>
            _strategy.Schedule(flights, runways, windowStart, windowEnd);
    }
}
