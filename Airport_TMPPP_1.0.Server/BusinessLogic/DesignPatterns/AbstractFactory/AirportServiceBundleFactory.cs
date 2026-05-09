namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.AbstractFactory
{
    public interface IAirportLoungeAccess
    {
        string Name { get; }
        string Benefits { get; }
    }

    public interface IAirportTransfer
    {
        string Type { get; }
        string Details { get; }
    }

    public interface ITravelInsurance
    {
        string Plan { get; }
        string CoverageSummary { get; }
    }

    public interface IAirportServiceBundleFactory
    {
        IAirportLoungeAccess CreateLoungeAccess();
        IAirportTransfer CreateTransfer();
        ITravelInsurance CreateInsurance();
    }

    public sealed class BusinessTravelerFactory : IAirportServiceBundleFactory
    {
        public IAirportLoungeAccess CreateLoungeAccess() =>
            new AirportLoungeAccess("Executive Lounge", "Quiet work pods, private meeting rooms, fast Wi-Fi.");

        public IAirportTransfer CreateTransfer() =>
            new AirportTransfer("Private Sedan", "Direct terminal pickup with driver assistance.");

        public ITravelInsurance CreateInsurance() =>
            new TravelInsurance("Business Premium", "High luggage coverage and flight-delay compensation.");
    }

    public sealed class FamilyTravelerFactory : IAirportServiceBundleFactory
    {
        public IAirportLoungeAccess CreateLoungeAccess() =>
            new AirportLoungeAccess("Family Lounge", "Kids corner, snacks, stroller-friendly seating.");

        public IAirportTransfer CreateTransfer() =>
            new AirportTransfer("Family Shuttle", "Shared shuttle with child-seat options.");

        public ITravelInsurance CreateInsurance() =>
            new TravelInsurance("Family Comfort", "Medical coverage and cancellation protection for all members.");
    }

    internal sealed record AirportLoungeAccess(string Name, string Benefits) : IAirportLoungeAccess;
    internal sealed record AirportTransfer(string Type, string Details) : IAirportTransfer;
    internal sealed record TravelInsurance(string Plan, string CoverageSummary) : ITravelInsurance;
}
