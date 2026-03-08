using System.Threading;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Singleton
{
    /// <summary>
    /// Example of a thread‑safe Singleton that represents a shared database
    /// connection manager. In a real system this would wrap an actual
    /// IDbConnection or DbContext; here it simply simulates a single,
    /// reusable connection object.
    /// </summary>
    public sealed class DatabaseConnectionManager
    {
        // Lazy<T> uses lazy, thread‑safe initialization and guarantees that
        // only one instance of DatabaseConnectionManager is ever created.
        private static readonly Lazy<DatabaseConnectionManager> _instance =
            new(() => new DatabaseConnectionManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static DatabaseConnectionManager Instance => _instance.Value;

        private readonly object _syncRoot = new();

        // For demo purposes we expose some simple properties that could be
        // shared across the application.
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

        /// <summary>
        /// Allows one‑time or occasional reconfiguration of the underlying
        /// connection settings. Thread‑safe via locking around mutation.
        /// </summary>
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

        /// <summary>
        /// Simulates executing a SQL command using the single shared connection.
        /// </summary>
        public void ExecuteCommand(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL command is required.", nameof(sql));

            lock (_syncRoot)
            {
                // In a real implementation, this would use an actual database connection.
                // Here we just update the last‑used timestamp to show shared state.
                LastUsedUtc = DateTime.UtcNow;
            }
        }
    }
}

