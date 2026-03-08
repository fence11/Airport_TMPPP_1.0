namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.FactoryMethod
{

    // Client requests a transport type (Airplane, Train, or Bus)
    public enum TransportType
    {
        Airplane,
        Train,
        Bus
    }


    /// The Creator that defines the factory method
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

        // which subclasses override to create specific transport products
        protected abstract ITransport CreateTransport();

        // making the high‑level operation virtual allows subclasses or decorators
        // to override pricing logic (e.g. ticket class modifiers)
        public virtual ReservationSummary MakeReservation()
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

    // Concrete Creator for airplane reservations
    public sealed class AirplaneReservation : TransportReservation
    {
        public AirplaneReservation(string customerName, int distanceKm)
            : base(customerName, distanceKm)
        {
        }

        protected override ITransport CreateTransport() => new AirplaneTransport();
    }


    // Concrete Creator for train reservations
    public sealed class TrainReservation : TransportReservation
    {
        public TrainReservation(string customerName, int distanceKm)
            : base(customerName, distanceKm)
        {
        }

        protected override ITransport CreateTransport() => new TrainTransport();
    }

    // Concrete Creator for bus reservations.
    public sealed class BusReservation : TransportReservation
    {
        public BusReservation(string customerName, int distanceKm)
            : base(customerName, distanceKm)
        {
        }

        protected override ITransport CreateTransport() => new BusTransport();
    }

    public sealed record ReservationSummary(
        string CustomerName,
        string TransportName,
        int DistanceKm,
        decimal Price,
        string TransportDescription);

    // ticket classes for abstract factory
    public enum TicketClass
    {
        Economy,
        FirstClass
    }

    // abstract factory interface - creates transport reservations depending on ticket class
    public interface IReservationFactory
    {
        TransportReservation CreateReservation(
            TransportType type,
            string customerName,
            int distanceKm);
    }

    // econ/first‑class decorators that modify pricing
    public class EconomyTicketReservation : TransportReservation
    {
        private readonly TransportReservation _inner;
        public EconomyTicketReservation(TransportReservation inner)
            : base(inner.CustomerName, inner.DistanceKm)
        {
            _inner = inner;
        }

        protected override ITransport CreateTransport()
        {
            throw new NotSupportedException();
        }

        public override ReservationSummary MakeReservation()
        {
            var summary = _inner.MakeReservation();
            return summary with { Price = summary.Price * 0.9m };
        }
    }

    public class FirstClassTicketReservation : TransportReservation
    {
        private readonly TransportReservation _inner;
        public FirstClassTicketReservation(TransportReservation inner)
            : base(inner.CustomerName, inner.DistanceKm)
        {
            _inner = inner;
        }

        protected override ITransport CreateTransport()
        {
            throw new NotSupportedException();
        }

        public override ReservationSummary MakeReservation()
        {
            var summary = _inner.MakeReservation();
            return summary with { Price = summary.Price * 1.5m };
        }
    }

    // concrete factories implementing the abstract factory
    public class EconomyReservationFactory : IReservationFactory
    {
        public TransportReservation CreateReservation(
            TransportType type,
            string customerName,
            int distanceKm)
        {
            var baseRes = TransportReservationFactory.CreateReservation(type, customerName, distanceKm);
            return new EconomyTicketReservation(baseRes);
        }
    }

    public class FirstClassReservationFactory : IReservationFactory
    {
        public TransportReservation CreateReservation(
            TransportType type,
            string customerName,
            int distanceKm)
        {
            var baseRes = TransportReservationFactory.CreateReservation(type, customerName, distanceKm);
            return new FirstClassTicketReservation(baseRes);
        }
    }

    // factory takes transporttype and other reservation details, creates the appropriate reservation subclass, and returns it as a base class reference
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

