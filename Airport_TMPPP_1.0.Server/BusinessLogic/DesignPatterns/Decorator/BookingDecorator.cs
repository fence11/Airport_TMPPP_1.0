namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Decorator
{
    public interface IFlightBooking
    {
        string Describe();
        decimal GetTotalPrice();
    }

    public sealed class BaseFlightBooking : IFlightBooking
    {
        private readonly string _flightNumber;
        private readonly decimal _basePrice;

        public BaseFlightBooking(string flightNumber, decimal basePrice)
        {
            _flightNumber = flightNumber;
            _basePrice = basePrice;
        }

        public string Describe() => $"Flight {_flightNumber} base booking";
        public decimal GetTotalPrice() => _basePrice;
    }

    public abstract class BookingDecorator : IFlightBooking
    {
        protected readonly IFlightBooking Inner;

        protected BookingDecorator(IFlightBooking inner) =>
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));

        public abstract string Describe();
        public abstract decimal GetTotalPrice();
    }

    public sealed class PriorityBoardingDecorator : BookingDecorator
    {
        private const decimal ExtraCost = 35m;

        public PriorityBoardingDecorator(IFlightBooking inner) : base(inner) { }

        public override string Describe() => $"{Inner.Describe()} + Priority Boarding";
        public override decimal GetTotalPrice() => Inner.GetTotalPrice() + ExtraCost;
    }

    public sealed class LoungeAccessDecorator : BookingDecorator
    {
        private const decimal ExtraCost = 55m;

        public LoungeAccessDecorator(IFlightBooking inner) : base(inner) { }

        public override string Describe() => $"{Inner.Describe()} + Lounge Access";
        public override decimal GetTotalPrice() => Inner.GetTotalPrice() + ExtraCost;
    }
}
