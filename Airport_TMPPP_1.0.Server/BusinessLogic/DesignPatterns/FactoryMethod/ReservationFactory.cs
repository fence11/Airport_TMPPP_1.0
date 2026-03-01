namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.FactoryMethod
{
    /// <summary>
    /// Enum used by clients to request a reservation type
    /// without depending on concrete transport classes.
    /// </summary>
    public enum TransportType
    {
        Airplane,
        Train,
        Bus
    }

    /// <summary>
    /// Creator base class in the Factory Method pattern.
    /// Exposes a factory method that subclasses override to
    /// instantiate concrete transports.
    /// </summary>
    public abstract class TransportReservation
    {
        public string CustomerName { get; }
        public int DistanceKm { get; }

        protected TransportReservation(string customerName, int distanceKm)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                throw new ArgumentException("Customer name is required.", nameof(customerName));
            if (distanceKm <= 0)
                throw new ArgumentOutOfRangeException(nameof(distanceKm), "Distance must be positive.");

            CustomerName = customerName;
            DistanceKm = distanceKm;
        }

        /// <summary>
        /// Factory Method – subclasses decide which concrete ITransport
        /// implementation will be created.
        /// </summary>
        protected abstract ITransport CreateTransport();

        /// <summary>
        /// High-level operation that uses the factory method.
        /// The client works with the ITransport abstraction only.
        /// </summary>
        public ReservationSummary MakeReservation()
        {
            var transport = CreateTransport();
            var price = transport.CalculatePrice(DistanceKm);

            return new ReservationSummary(
                CustomerName,
                transport.Name,
                DistanceKm,
                price,
                transport.Description);
        }
    }

    /// <summary>
    /// Concrete Creator for airplane reservations.
    /// </summary>
    public sealed class AirplaneReservation : TransportReservation
    {
        public AirplaneReservation(string customerName, int distanceKm)
            : base(customerName, distanceKm)
        {
        }

        protected override ITransport CreateTransport() => new AirplaneTransport();
    }

    /// <summary>
    /// Concrete Creator for train reservations.
    /// </summary>
    public sealed class TrainReservation : TransportReservation
    {
        public TrainReservation(string customerName, int distanceKm)
            : base(customerName, distanceKm)
        {
        }

        protected override ITransport CreateTransport() => new TrainTransport();
    }

    /// <summary>
    /// Concrete Creator for bus reservations.
    /// </summary>
    public sealed class BusReservation : TransportReservation
    {
        public BusReservation(string customerName, int distanceKm)
            : base(customerName, distanceKm)
        {
        }

        protected override ITransport CreateTransport() => new BusTransport();
    }

    /// <summary>
    /// Simple DTO returned to clients when a reservation is made.
    /// </summary>
    public sealed record ReservationSummary(
        string CustomerName,
        string TransportName,
        int DistanceKm,
        decimal Price,
        string TransportDescription);

    /// <summary>
    /// Optional helper class that selects the correct concrete Creator
    /// based on a TransportType value. This keeps higher layers
    /// independent from concrete reservation classes.
    /// </summary>
    public static class TransportReservationFactory
    {
        public static TransportReservation CreateReservation(
            TransportType type,
            string customerName,
            int distanceKm)
        {
            return type switch
            {
                TransportType.Airplane => new AirplaneReservation(customerName, distanceKm),
                TransportType.Train => new TrainReservation(customerName, distanceKm),
                TransportType.Bus => new BusReservation(customerName, distanceKm),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported transport type.")
            };
        }
    }
}

