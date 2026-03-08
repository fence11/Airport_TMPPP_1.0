namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.FactoryMethod
{
    // Product interface for the Factory Method pattern.

    public interface ITransport
    {
        string Name { get; }
        string Description { get; }
        decimal CalculatePrice(int distanceKm);
    }
}

