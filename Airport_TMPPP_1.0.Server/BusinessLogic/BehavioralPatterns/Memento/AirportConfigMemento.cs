namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Memento
{
    // ── Memento ───────────────────────────────────────────────────────────

    public sealed class AirportConfigMemento
    {
        public string MementoId { get; } = Guid.NewGuid().ToString("N")[..8];
        public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;
        public string Label { get; }

        // Captured state (immutable snapshot)
        internal string TerminalLayout { get; }
        internal int ActiveRunways { get; }
        internal int MaxDailyFlights { get; }
        internal bool IsInternationalEnabled { get; }
        internal IReadOnlyList<string> ActiveGates { get; }
        internal string SecurityLevel { get; }
        internal string Notes { get; }

        internal AirportConfigMemento(
            string label,
            string terminalLayout,
            int activeRunways,
            int maxDailyFlights,
            bool isInternationalEnabled,
            IEnumerable<string> activeGates,
            string securityLevel,
            string notes)
        {
            Label = label;
            TerminalLayout = terminalLayout;
            ActiveRunways = activeRunways;
            MaxDailyFlights = maxDailyFlights;
            IsInternationalEnabled = isInternationalEnabled;
            ActiveGates = activeGates.ToList().AsReadOnly();
            SecurityLevel = securityLevel;
            Notes = notes;
        }

        // Safe public view for API responses
        public AirportConfigSnapshot ToSnapshot() => new(
            MementoId,
            Label,
            CreatedAtUtc,
            TerminalLayout,
            ActiveRunways,
            MaxDailyFlights,
            IsInternationalEnabled,
            ActiveGates,
            SecurityLevel,
            Notes);
    }

    public sealed record AirportConfigSnapshot(
        string MementoId,
        string Label,
        DateTime CreatedAtUtc,
        string TerminalLayout,
        int ActiveRunways,
        int MaxDailyFlights,
        bool IsInternationalEnabled,
        IReadOnlyList<string> ActiveGates,
        string SecurityLevel,
        string Notes);

    // ── Originator ────────────────────────────────────────────────────────

    public sealed class AirportConfiguration
    {
        public string TerminalLayout { get; set; } = "Standard";
        public int ActiveRunways { get; set; } = 2;
        public int MaxDailyFlights { get; set; } = 120;
        public bool IsInternationalEnabled { get; set; } = true;
        public List<string> ActiveGates { get; set; } = new() { "A1", "A2", "B1", "B2", "C1" };
        public string SecurityLevel { get; set; } = "Standard";
        public string Notes { get; set; } = string.Empty;

        public AirportConfigMemento Save(string label) =>
            new(label,
                TerminalLayout,
                ActiveRunways,
                MaxDailyFlights,
                IsInternationalEnabled,
                ActiveGates,
                SecurityLevel,
                Notes);

        public void Restore(AirportConfigMemento memento)
        {
            TerminalLayout = memento.TerminalLayout;
            ActiveRunways = memento.ActiveRunways;
            MaxDailyFlights = memento.MaxDailyFlights;
            IsInternationalEnabled = memento.IsInternationalEnabled;
            ActiveGates = memento.ActiveGates.ToList();
            SecurityLevel = memento.SecurityLevel;
            Notes = memento.Notes;
        }

        public AirportConfigSnapshot CurrentSnapshot() => new(
            "current",
            "Current",
            DateTime.UtcNow,
            TerminalLayout,
            ActiveRunways,
            MaxDailyFlights,
            IsInternationalEnabled,
            ActiveGates.AsReadOnly(),
            SecurityLevel,
            Notes);
    }

    // ── Caretaker ─────────────────────────────────────────────────────────

    public sealed class AirportConfigCaretaker
    {
        private readonly AirportConfiguration _config;
        private readonly List<AirportConfigMemento> _history = new();

        public AirportConfigCaretaker()
        {
            _config = new AirportConfiguration();
            // Seed a baseline snapshot
            SaveSnapshot("Initial configuration");
        }

        public AirportConfigSnapshot GetCurrentConfig() => _config.CurrentSnapshot();

        public void ApplyChanges(
            string? terminalLayout = null,
            int? activeRunways = null,
            int? maxDailyFlights = null,
            bool? isInternationalEnabled = null,
            List<string>? activeGates = null,
            string? securityLevel = null,
            string? notes = null)
        {
            if (terminalLayout != null) _config.TerminalLayout = terminalLayout;
            if (activeRunways.HasValue) _config.ActiveRunways = activeRunways.Value;
            if (maxDailyFlights.HasValue) _config.MaxDailyFlights = maxDailyFlights.Value;
            if (isInternationalEnabled.HasValue) _config.IsInternationalEnabled = isInternationalEnabled.Value;
            if (activeGates != null) _config.ActiveGates = activeGates;
            if (securityLevel != null) _config.SecurityLevel = securityLevel;
            if (notes != null) _config.Notes = notes;
        }

        public string SaveSnapshot(string label)
        {
            var memento = _config.Save(label);
            _history.Add(memento);
            return memento.MementoId;
        }

        public bool RestoreSnapshot(string mementoId)
        {
            var memento = _history.FirstOrDefault(m => m.MementoId == mementoId);
            if (memento is null) return false;
            _config.Restore(memento);
            return true;
        }

        public bool RestoreLast()
        {
            if (_history.Count < 2) return false;
            var previous = _history[^2];
            _config.Restore(previous);
            // Remove the last snapshot (we rolled back)
            _history.RemoveAt(_history.Count - 1);
            return true;
        }

        public IReadOnlyList<AirportConfigSnapshot> GetHistory() =>
            _history.Select(m => m.ToSnapshot()).Reverse().ToList().AsReadOnly();
    }
}
