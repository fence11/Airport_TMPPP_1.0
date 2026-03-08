using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Builder;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Prototype;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Singleton;
using Microsoft.AspNetCore.Mvc;

namespace Airport_TMPPP_1._0.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DesignPatternsController : ControllerBase
    {
        /// <summary>
        /// Demonstrates the Builder pattern by constructing a weekend city-break package.
        /// </summary>
        [HttpGet("builder/weekend")]
        public ActionResult<TravelPackageDto> GetWeekendCityBreak(
            [FromQuery] string customerName,
            [FromQuery] string city)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                return BadRequest("Customer name is required.");
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("City is required.");

            var director = new TravelDirector();
            var builder = new CustomTravelPackageBuilder();
            var package = director.CreateWeekendCityBreak(builder, customerName, city);

            return Ok(TravelPackageDto.FromDomain(package, "Builder: Weekend city break"));
        }

        /// <summary>
        /// Demonstrates the Builder pattern by constructing an all-inclusive beach holiday.
        /// </summary>
        [HttpGet("builder/beach")]
        public ActionResult<TravelPackageDto> GetBeachHoliday(
            [FromQuery] string customerName,
            [FromQuery] string resort)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                return BadRequest("Customer name is required.");
            if (string.IsNullOrWhiteSpace(resort))
                return BadRequest("Resort is required.");

            var director = new TravelDirector();
            var builder = new CustomTravelPackageBuilder();
            var package = director.CreateAllInclusiveBeachHoliday(builder, customerName, resort);

            return Ok(TravelPackageDto.FromDomain(package, "Builder: All-inclusive beach holiday"));
        }

        /// <summary>
        /// Demonstrates the Prototype pattern by creating a base document and cloning it.
        /// </summary>
        [HttpGet("prototype")]
        public ActionResult<PrototypeDemoResponse> GetPrototypeDemo()
        {
            var baseDocument = new TravelDocument(
                title: "Reservation Confirmation",
                passengerName: "Template Passenger",
                destination: "Demo City",
                travelDate: DateTime.Today.AddMonths(1),
                metadata: new Dictionary<string, string>
                {
                    ["DocumentType"] = "Reservation",
                    ["Language"] = "EN"
                },
                notes: new List<string> { "Base template created on server." });

            var deepClone = baseDocument.DeepClone();
            deepClone.Notes.Add("Clone customized for demonstration.");

            var response = new PrototypeDemoResponse
            {
                Original = TravelDocumentDto.FromDomain(baseDocument),
                Clone = TravelDocumentDto.FromDomain(deepClone)
            };

            return Ok(response);
        }

        /// <summary>
        /// Demonstrates the Singleton pattern by returning information about the shared
        /// database connection manager instance.
        /// </summary>
        [HttpGet("singleton")]
        public ActionResult<DatabaseConnectionInfoDto> GetSingletonInfo()
        {
            var manager = DatabaseConnectionManager.Instance;

            // Simulate some work on the shared instance.
            manager.ExecuteCommand("SELECT 1");

            var dto = new DatabaseConnectionInfoDto
            {
                ConnectionId = manager.ConnectionId,
                ConnectionString = manager.ConnectionString,
                LastUsedUtc = manager.LastUsedUtc
            };

            return Ok(dto);
        }
    }

    public sealed class TravelPackageDto
    {
        public string DemoTitle { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Transport { get; set; } = string.Empty;
        public string Accommodation { get; set; } = string.Empty;
        public IReadOnlyList<string> Activities { get; set; } = Array.Empty<string>();
        public decimal Price { get; set; }

        public static TravelPackageDto FromDomain(TravelPackage package, string demoTitle) =>
            new()
            {
                DemoTitle = demoTitle,
                CustomerName = package.CustomerName,
                Destination = package.Destination,
                StartDate = package.StartDate,
                EndDate = package.EndDate,
                Transport = package.Transport,
                Accommodation = package.Accommodation,
                Activities = package.Activities,
                Price = package.Price
            };
    }

    public sealed class TravelDocumentDto
    {
        public string Title { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime TravelDate { get; set; }
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public IList<string> Notes { get; set; } = new List<string>();

        public static TravelDocumentDto FromDomain(TravelDocument document) =>
            new()
            {
                Title = document.Title,
                PassengerName = document.PassengerName,
                Destination = document.Destination,
                TravelDate = document.TravelDate,
                Metadata = new Dictionary<string, string>(document.Metadata),
                Notes = new List<string>(document.Notes)
            };
    }

    public sealed class PrototypeDemoResponse
    {
        public TravelDocumentDto Original { get; set; } = new();
        public TravelDocumentDto Clone { get; set; } = new();
    }

    public sealed class DatabaseConnectionInfoDto
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public DateTime LastUsedUtc { get; set; }
    }
}

