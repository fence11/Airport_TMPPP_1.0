using System.Threading;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Singleton
{
    public sealed class DatabaseConnectionManager
    {
        // Lazy<T> uses lazy, thread‑safe initialization and guarantees that
        // only one instance of DatabaseConnectionManager is ever created.
        private static readonly Lazy<DatabaseConnectionManager> _instance =
            new(() => new DatabaseConnectionManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static DatabaseConnectionManager Instance => _instance.Value;

        private readonly object _syncRoot = new();
        public string ConnectionString { get; private set; }
        public string ConnectionId { get; }
        public DateTime LastUsedUtc { get; private set; }

        // Private constructor prevents external instantiation.
        private DatabaseConnectionManager()
        {
            ConnectionId = Guid.NewGuid().ToString("N");
            ConnectionString = "Host=localhost;Port=5432;Database=Airport;Username=app;Password=secret";
            LastUsedUtc = DateTime.UtcNow;
        }

        public void Configure(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required.", nameof(connectionString));

            lock (_syncRoot)
            {
                ConnectionString = connectionString;
                LastUsedUtc = DateTime.UtcNow;
            }
        }


        public void ExecuteCommand(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL command is required.", nameof(sql));

            lock (_syncRoot)
            {
                // update the last‑used timestamp to show shared state, not database connection.
                LastUsedUtc = DateTime.UtcNow;
            }
        }
    }
}

