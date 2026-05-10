using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Iterator
{
    public interface IAirportIterator<T>
    {
        bool HasNext();
        T Next();
        void Reset();
        T Current { get; }
        int CurrentIndex { get; }
        int TotalCount { get; }
    }

    // ── Flight Iterator ───────────────────────────────────────────────────

    public sealed class FlightIterator : IAirportIterator<FlightIteratorItem>
    {
        private readonly IReadOnlyList<FlightIteratorItem> _items;
        private int _index = -1;

        public FlightIterator(IEnumerable<FlightIteratorItem> flights) =>
            _items = flights.ToList().AsReadOnly();

        public bool HasNext() => _index + 1 < _items.Count;
        public FlightIteratorItem Next() => _items[++_index];
        public void Reset() => _index = -1;
        public FlightIteratorItem Current => _index >= 0 ? _items[_index] : throw new InvalidOperationException("Call Next() first.");
        public int CurrentIndex => _index;
        public int TotalCount => _items.Count;
    }

    public sealed record FlightIteratorItem(
        int Id,
        string FlightNumber,
        int AirportId,
        string AirportName,
        string AirportCode,
        DateTime CreatedAt);

    // ── Filtered Flight Iterator ──────────────────────────────────────────

    public sealed class FilteredFlightIterator : IAirportIterator<FlightIteratorItem>
    {
        private readonly IReadOnlyList<FlightIteratorItem> _items;
        private int _index = -1;
        public string FilterDescription { get; }

        public FilteredFlightIterator(
            IEnumerable<FlightIteratorItem> flights,
            Func<FlightIteratorItem, bool> predicate,
            string filterDescription)
        {
            _items = flights.Where(predicate).ToList().AsReadOnly();
            FilterDescription = filterDescription;
        }

        public bool HasNext() => _index + 1 < _items.Count;
        public FlightIteratorItem Next() => _items[++_index];
        public void Reset() => _index = -1;
        public FlightIteratorItem Current => _index >= 0 ? _items[_index] : throw new InvalidOperationException("Call Next() first.");
        public int CurrentIndex => _index;
        public int TotalCount => _items.Count;
    }

    // ── Airport Facility Iterator ─────────────────────────────────────────

    public sealed record AirportFacility(
        string FacilityId,
        string Name,
        string FacilityType,  // Gate, Runway, Terminal, Lounge
        string Zone,
        bool IsOperational,
        string? Notes);

    public sealed class FacilityIterator : IAirportIterator<AirportFacility>
    {
        private readonly IReadOnlyList<AirportFacility> _items;
        private int _index = -1;

        public FacilityIterator(IEnumerable<AirportFacility> facilities) =>
            _items = facilities.ToList().AsReadOnly();

        public bool HasNext() => _index + 1 < _items.Count;
        public AirportFacility Next() => _items[++_index];
        public void Reset() => _index = -1;
        public AirportFacility Current => _index >= 0 ? _items[_index] : throw new InvalidOperationException("Call Next() first.");
        public int CurrentIndex => _index;
        public int TotalCount => _items.Count;
    }

    // ── Iterator Factory ──────────────────────────────────────────────────

    public static class AirportIteratorFactory
    {
        public static FlightIterator CreateFlightIterator(
            IEnumerable<Flight> flights,
            IEnumerable<Airport> airports)
        {
            var airportMap = airports.ToDictionary(a => a.Id);
            var items = flights.Select(f =>
            {
                airportMap.TryGetValue(f.AirportId, out var airport);
                return new FlightIteratorItem(
                    f.Id,
                    f.FlightNumber,
                    f.AirportId,
                    airport?.Name ?? "Unknown",
                    airport?.Code ?? "???",
                    f.CreatedAt);
            });
            return new FlightIterator(items);
        }

        public static FilteredFlightIterator CreateAirportFlightIterator(
            IEnumerable<Flight> flights,
            IEnumerable<Airport> airports,
            string airportCode)
        {
            var airportMap = airports.ToDictionary(a => a.Id);
            var items = flights.Select(f =>
            {
                airportMap.TryGetValue(f.AirportId, out var airport);
                return new FlightIteratorItem(
                    f.Id, f.FlightNumber, f.AirportId,
                    airport?.Name ?? "Unknown",
                    airport?.Code ?? "???",
                    f.CreatedAt);
            });

            return new FilteredFlightIterator(
                items,
                fi => fi.AirportCode.Equals(airportCode, StringComparison.OrdinalIgnoreCase),
                $"Flights at airport {airportCode.ToUpper()}");
        }

        public static FacilityIterator CreateFacilityIterator(string? filterType = null)
        {
            var allFacilities = new List<AirportFacility>
            {
                new("G-A1",  "Gate A1",  "Gate",     "Terminal A", true,  "International departures"),
                new("G-A2",  "Gate A2",  "Gate",     "Terminal A", true,  null),
                new("G-A3",  "Gate A3",  "Gate",     "Terminal A", false, "Under maintenance until 2026-06-01"),
                new("G-B1",  "Gate B1",  "Gate",     "Terminal B", true,  "Domestic"),
                new("G-B2",  "Gate B2",  "Gate",     "Terminal B", true,  "Domestic"),
                new("G-C1",  "Gate C1",  "Gate",     "Terminal C", true,  "Charter flights"),
                new("RW-1",  "Runway 01L","Runway",  "North",      true,  "Main landing runway"),
                new("RW-2",  "Runway 01R","Runway",  "North",      true,  "Main departure runway"),
                new("RW-3",  "Runway 09", "Runway",  "East",       false, "Closed for resurfacing"),
                new("T-A",   "Terminal A","Terminal", "Main",       true,  "International"),
                new("T-B",   "Terminal B","Terminal", "Main",       true,  "Domestic"),
                new("T-C",   "Terminal C","Terminal", "East",       true,  "Charter & private"),
                new("L-1",   "Business Lounge","Lounge","Terminal A",true, "Priority boarding lane"),
                new("L-2",   "Family Lounge",  "Lounge","Terminal B",true, "Kids play area included"),
                new("L-3",   "VIP Suite",      "Lounge","Terminal A",true, "By invitation only"),
            };

            if (filterType != null)
                return new FacilityIterator(
                    allFacilities.Where(f => f.FacilityType.Equals(filterType, StringComparison.OrdinalIgnoreCase)));

            return new FacilityIterator(allFacilities);
        }

        /// <summary>Materialises an iterator into a paged snapshot for the API.</summary>
        public static IteratorPageResult<T> Paginate<T>(
            IAirportIterator<T> iterator,
            int page,
            int pageSize) where T : class
        {
            iterator.Reset();
            int skip = (page - 1) * pageSize;
            int skipped = 0;
            var results = new List<T>();

            while (iterator.HasNext())
            {
                var item = iterator.Next();
                if (skipped++ < skip) continue;
                results.Add(item);
                if (results.Count >= pageSize) break;
            }

            return new IteratorPageResult<T>(
                results,
                page,
                pageSize,
                iterator.TotalCount,
                (int)Math.Ceiling(iterator.TotalCount / (double)pageSize));
        }
    }

    public sealed record IteratorPageResult<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}
