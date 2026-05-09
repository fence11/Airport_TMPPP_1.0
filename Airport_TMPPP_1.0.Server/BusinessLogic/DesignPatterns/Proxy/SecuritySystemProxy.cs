namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Proxy
{
    public interface ISensitiveAirportSystem
    {
        string Access(string actorRole, string query);
    }

    public sealed class AirTrafficControlSystem : ISensitiveAirportSystem
    {
        public string Access(string actorRole, string query) =>
            $"ATC feed delivered for '{query}' to role {actorRole}.";
    }

    public sealed class SecurityDatabaseSystem : ISensitiveAirportSystem
    {
        public string Access(string actorRole, string query) =>
            $"Security database returned '{query}' records to role {actorRole}.";
    }

    public sealed class SensitiveSystemProxy : ISensitiveAirportSystem
    {
        private readonly ISensitiveAirportSystem _realSystem;
        private readonly HashSet<string> _allowedRoles;

        public SensitiveSystemProxy(ISensitiveAirportSystem realSystem, IEnumerable<string> allowedRoles)
        {
            _realSystem = realSystem ?? throw new ArgumentNullException(nameof(realSystem));
            _allowedRoles = new HashSet<string>(allowedRoles.Select(r => r.Trim().ToLowerInvariant()));
        }

        public string Access(string actorRole, string query)
        {
            var normalizedRole = actorRole.Trim().ToLowerInvariant();
            if (!_allowedRoles.Contains(normalizedRole))
                return $"Access denied for role '{actorRole}'.";

            return _realSystem.Access(actorRole, query);
        }
    }
}
