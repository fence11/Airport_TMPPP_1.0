using Airport_TMPPP_1._0.Server.BusinessLogic.Common;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Entities
{

    /// Passenger entity following Single Responsibility Principle (SRP)
    /// Represents only passenger data and behavior

    public class Passenger : AuditableEntity
    {
        public string FirstName { get; private set; } = null!;
        public string LastName { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string? PhoneNumber { get; private set; }

        private Passenger() { } // EF Core

        public Passenger(string firstName, string lastName, string email, string? phoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be empty", nameof(firstName));
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be empty", nameof(lastName));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
        }

        public void UpdateContactInfo(string? phoneNumber)
        {
            PhoneNumber = phoneNumber;
            MarkUpdated();
        }
        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
                throw new Exception("First name required");

            if (string.IsNullOrWhiteSpace(Email))
                throw new Exception("Email required");
        }

    }
}
