namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Builder
{
    public sealed class TravelPackage
    {
        public string CustomerName { get; }
        public string Destination { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public string Transport { get; }
        public string Accommodation { get; }
        public IReadOnlyList<string> Activities { get; }
        public decimal Price { get; }

        internal TravelPackage(
            string customerName,
            string destination,
            DateTime startDate,
            DateTime endDate,
            string transport,
            string accommodation,
            IReadOnlyList<string> activities,
            decimal price)
        {
            CustomerName = customerName;
            Destination = destination;
            StartDate = startDate;
            EndDate = endDate;
            Transport = transport;
            Accommodation = accommodation;
            Activities = activities;
            Price = price;
        }
    }


    public interface ITravelPackageBuilder
    {
        ITravelPackageBuilder ForCustomer(string customerName);
        ITravelPackageBuilder ToDestination(string destination);
        ITravelPackageBuilder WithDates(DateTime startDate, DateTime endDate);
        ITravelPackageBuilder WithTransport(string transportDescription);
        ITravelPackageBuilder WithAccommodation(string accommodationDescription);
        ITravelPackageBuilder AddActivity(string activity);
        ITravelPackageBuilder WithBasePrice(decimal price);

        TravelPackage Build();
    }

    public sealed class CustomTravelPackageBuilder : ITravelPackageBuilder
    {
        private string? _customerName;
        private string? _destination;
        private DateTime _startDate;
        private DateTime _endDate;
        private string? _transport;
        private string? _accommodation;
        private readonly List<string> _activities = new();
        private decimal _price;

        public ITravelPackageBuilder ForCustomer(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                throw new ArgumentException("Customer name is required.", nameof(customerName));

            _customerName = customerName;
            return this;
        }

        public ITravelPackageBuilder ToDestination(string destination)
        {
            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException("Destination is required.", nameof(destination));

            _destination = destination;
            return this;
        }

        public ITravelPackageBuilder WithDates(DateTime startDate, DateTime endDate)
        {
            if (endDate <= startDate)
                throw new ArgumentException("End date must be after start date.", nameof(endDate));

            _startDate = startDate;
            _endDate = endDate;
            return this;
        }

        public ITravelPackageBuilder WithTransport(string transportDescription)
        {
            if (string.IsNullOrWhiteSpace(transportDescription))
                throw new ArgumentException("Transport description is required.", nameof(transportDescription));

            _transport = transportDescription;
            return this;
        }

        public ITravelPackageBuilder WithAccommodation(string accommodationDescription)
        {
            if (string.IsNullOrWhiteSpace(accommodationDescription))
                throw new ArgumentException("Accommodation description is required.", nameof(accommodationDescription));

            _accommodation = accommodationDescription;
            return this;
        }

        public ITravelPackageBuilder AddActivity(string activity)
        {
            if (string.IsNullOrWhiteSpace(activity))
                throw new ArgumentException("Activity is required.", nameof(activity));

            _activities.Add(activity);
            return this;
        }

        public ITravelPackageBuilder WithBasePrice(decimal price)
        {
            if (price <= 0)
                throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive.");

            _price = price;
            return this;
        }

        public TravelPackage Build()
        {
            if (_customerName is null)
                throw new InvalidOperationException("Customer name must be specified.");
            if (_destination is null)
                throw new InvalidOperationException("Destination must be specified.");
            if (_transport is null)
                throw new InvalidOperationException("Transport must be specified.");
            if (_accommodation is null)
                throw new InvalidOperationException("Accommodation must be specified.");
            if (_startDate == default || _endDate == default)
                throw new InvalidOperationException("Travel dates must be specified.");
            if (_price <= 0)
                throw new InvalidOperationException("Base price must be greater than zero.");

            return new TravelPackage(
                _customerName,
                _destination,
                _startDate,
                _endDate,
                _transport,
                _accommodation,
                _activities.AsReadOnly(),
                _price);
        }
    }


    // Director responsible for orchestrating the construction steps to produce pre‑defined travel products.
    public sealed class TravelDirector
    {
        public TravelPackage CreateWeekendCityBreak(
            ITravelPackageBuilder builder,
            string customerName,
            string city)
        {
            var start = DateTime.Today.AddDays(7);
            var end = start.AddDays(2);

            return builder
                .ForCustomer(customerName)
                .ToDestination(city)
                .WithDates(start, end)
                .WithTransport("Round‑trip airplane ticket")
                .WithAccommodation("3‑star city‑center hotel, breakfast included")
                .AddActivity("Guided city tour")
                .AddActivity("Museum pass")
                .WithBasePrice(499m)
                .Build();
        }

        public TravelPackage CreateAllInclusiveBeachHoliday(
            ITravelPackageBuilder builder,
            string customerName,
            string resort)
        {
            var start = DateTime.Today.AddMonths(1);
            var end = start.AddDays(7);

            return builder
                .ForCustomer(customerName)
                .ToDestination(resort)
                .WithDates(start, end)
                .WithTransport("Charter flight")
                .WithAccommodation("5‑star beach resort, all‑inclusive")
                .AddActivity("Snorkeling excursion")
                .AddActivity("Spa day pass")
                .WithBasePrice(1899m)
                .Build();
        }
    }
}

