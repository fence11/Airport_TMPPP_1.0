namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Flyweight
{
    public interface IAirportResourceFlyweight
    {
        string ResourceType { get; }
        string Zone { get; }
        string RenderUsage(string contextId, string flightCode, DateTime slotUtc);
    }

    public sealed class AirportResourceFlyweight : IAirportResourceFlyweight
    {
        public string ResourceType { get; }
        public string Zone { get; }

        public AirportResourceFlyweight(string resourceType, string zone)
        {
            ResourceType = resourceType;
            Zone = zone;
        }

        public string RenderUsage(string contextId, string flightCode, DateTime slotUtc) =>
            $"{ResourceType} {contextId} in zone {Zone} reserved for {flightCode} at {slotUtc:O}";
    }

    public sealed class AirportResourceFactory
    {
        private readonly Dictionary<string, IAirportResourceFlyweight> _cache = new();

        public IAirportResourceFlyweight GetResource(string resourceType, string zone)
        {
            var key = $"{resourceType.Trim().ToUpperInvariant()}::{zone.Trim().ToUpperInvariant()}";
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var created = new AirportResourceFlyweight(resourceType, zone);
            _cache[key] = created;
            return created;
        }

        public int SharedObjectCount => _cache.Count;
    }

    public sealed record AirportResourceAssignment(
        string ContextId,
        string FlightCode,
        DateTime SlotUtc,
        IAirportResourceFlyweight Resource);
}
