namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.FactoryMethod
{
    /// <summary>
    /// Product interface for the Factory Method pattern.
    /// Represents a generic transport that can be reserved.
    /// </summary>
    public interface ITransport
    {
        string Name { get; }
        string Description { get; }

        /// <summary>
        /// Calculates a simple price for the reservation.
        /// </summary>
        /// <param name="distanceKm">Trip distance in kilometers.</param>
        /// <returns>Total price.</returns>
        decimal CalculatePrice(int distanceKm);
    }
}

