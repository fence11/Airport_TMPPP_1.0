namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.FactoryMethod
{

    // Concrete airplane transport product

    public sealed class AirplaneTransport : ITransport
    {
        public string Name => "Airplane";
        public string Description => "Fast air transport suitable for long distances.";

        public decimal CalculatePrice(int distanceKm)
        {
            if (distanceKm <= 0)
                throw new ArgumentOutOfRangeException(nameof(distanceKm), "Distance must be positive.");

            const decimal basePrice = 100m;
            const decimal perKm = 0.7m;
            return basePrice + perKm * distanceKm;
        }
    }

    // Concrete train transport product
    public sealed class TrainTransport : ITransport
    {
        public string Name => "Train";
        public string Description => "Comfortable rail transport for medium distances.";

        public decimal CalculatePrice(int distanceKm)
        {
            if (distanceKm <= 0)
                throw new ArgumentOutOfRangeException(nameof(distanceKm), "Distance must be positive.");

            const decimal basePrice = 50m;
            const decimal perKm = 0.4m;
            return basePrice + perKm * distanceKm;
        }
    }

    /// Concrete bus transport product
    public sealed class BusTransport : ITransport
    {
        public string Name => "Bus";
        public string Description => "Economic road transport for short to medium distances.";

        public decimal CalculatePrice(int distanceKm)
        {
            if (distanceKm <= 0)
                throw new ArgumentOutOfRangeException(nameof(distanceKm), "Distance must be positive.");

            const decimal basePrice = 20m;
            const decimal perKm = 0.2m;
            return basePrice + perKm * distanceKm;
        }
    }
}

