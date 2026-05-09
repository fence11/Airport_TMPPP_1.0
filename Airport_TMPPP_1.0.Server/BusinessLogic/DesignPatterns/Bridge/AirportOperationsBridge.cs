namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Bridge
{
    public interface IAirportImplementation
    {
        string AirportType { get; }
        string ExecuteLanding(string flightCode);
        string ExecuteSecurityScreening(string passengerName);
    }

    public sealed class InternationalAirportImplementation : IAirportImplementation
    {
        public string AirportType => "International";
        public string ExecuteLanding(string flightCode) => $"Runway assigned for international flight {flightCode}.";
        public string ExecuteSecurityScreening(string passengerName) => $"Enhanced border screening completed for {passengerName}.";
    }

    public sealed class DomesticAirportImplementation : IAirportImplementation
    {
        public string AirportType => "Domestic";
        public string ExecuteLanding(string flightCode) => $"Domestic gate prepared for flight {flightCode}.";
        public string ExecuteSecurityScreening(string passengerName) => $"Standard domestic screening completed for {passengerName}.";
    }

    public sealed class CargoAirportImplementation : IAirportImplementation
    {
        public string AirportType => "Cargo";
        public string ExecuteLanding(string flightCode) => $"Cargo apron slot opened for {flightCode}.";
        public string ExecuteSecurityScreening(string passengerName) => $"Cargo-crew screening completed for {passengerName}.";
    }

    public abstract class AirportOperation
    {
        protected readonly IAirportImplementation Implementation;

        protected AirportOperation(IAirportImplementation implementation) =>
            Implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));

        public abstract string Operate(string identifier);
    }

    public sealed class LandingOperation : AirportOperation
    {
        public LandingOperation(IAirportImplementation implementation) : base(implementation) { }
        public override string Operate(string identifier) => Implementation.ExecuteLanding(identifier);
    }

    public sealed class SecurityOperation : AirportOperation
    {
        public SecurityOperation(IAirportImplementation implementation) : base(implementation) { }
        public override string Operate(string identifier) => Implementation.ExecuteSecurityScreening(identifier);
    }
}
