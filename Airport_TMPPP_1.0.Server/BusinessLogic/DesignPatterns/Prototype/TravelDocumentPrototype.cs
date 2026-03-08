namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Prototype
{
    /// <summary>
    /// Generic prototype interface that exposes shallow and deep cloning operations.
    /// </summary>
    /// <typeparam name="T">Type of the object being cloned.</typeparam>
    public interface IPrototype<T>
    {
        T ShallowClone();
        T DeepClone();
    }

    /// <summary>
    /// Example of a document that might be used in an airport system
    /// (e.g., reservation confirmation, boarding document, etc.).
    /// </summary>
    public sealed class TravelDocument : IPrototype<TravelDocument>
    {
        public string Title { get; }
        public string PassengerName { get; }
        public string Destination { get; }
        public DateTime TravelDate { get; }

        // Reference‑type properties to demonstrate the difference
        // between shallow and deep cloning.
        public IDictionary<string, string> Metadata { get; }
        public IList<string> Notes { get; }

        public TravelDocument(
            string title,
            string passengerName,
            string destination,
            DateTime travelDate,
            IDictionary<string, string>? metadata = null,
            IList<string>? notes = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.", nameof(title));
            if (string.IsNullOrWhiteSpace(passengerName))
                throw new ArgumentException("Passenger name is required.", nameof(passengerName));
            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException("Destination is required.", nameof(destination));

            Title = title;
            PassengerName = passengerName;
            Destination = destination;
            TravelDate = travelDate;
            Metadata = metadata is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(metadata);
            Notes = notes is null
                ? new List<string>()
                : new List<string>(notes);
        }

        /// <summary>
        /// Creates a new TravelDocument that shares the same reference‑type instances
        /// (Metadata and Notes) with the original.
        /// </summary>
        public TravelDocument ShallowClone()
        {
            // MemberwiseClone performs a field‑by‑field copy; reference types are shared.
            return (TravelDocument)MemberwiseClone();
        }

        /// <summary>
        /// Creates a new TravelDocument with copies of the reference‑type properties,
        /// so modifications to Metadata or Notes on the clone will not affect the original.
        /// </summary>
        public TravelDocument DeepClone()
        {
            var metadataCopy = new Dictionary<string, string>(Metadata);
            var notesCopy = new List<string>(Notes);

            return new TravelDocument(
                Title,
                PassengerName,
                Destination,
                TravelDate,
                metadataCopy,
                notesCopy);
        }

        TravelDocument IPrototype<TravelDocument>.ShallowClone() => ShallowClone();
        TravelDocument IPrototype<TravelDocument>.DeepClone() => DeepClone();
    }

    /// <summary>
    /// Registry that stores named document prototypes and creates new instances from them.
    /// </summary>
    public sealed class TravelDocumentRegistry
    {
        private readonly Dictionary<string, TravelDocument> _prototypes =
            new(StringComparer.OrdinalIgnoreCase);

        public void Register(string key, TravelDocument prototype)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));
            if (prototype is null)
                throw new ArgumentNullException(nameof(prototype));

            _prototypes[key] = prototype;
        }

        public bool Unregister(string key) => _prototypes.Remove(key);

        public TravelDocument CreateFromPrototype(string key, bool deepClone = true)
        {
            if (!_prototypes.TryGetValue(key, out var prototype))
                throw new KeyNotFoundException($"No document prototype registered with key '{key}'.");

            return deepClone
                ? prototype.DeepClone()
                : prototype.ShallowClone();
        }
    }
}

