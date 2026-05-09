using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Adapter;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Bridge;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Composite;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Decorator;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Facade;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Flyweight;
using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Proxy;
using Microsoft.AspNetCore.Mvc;

namespace Airport_TMPPP_1._0.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StructuralPatternsController : ControllerBase
    {
        // ── ADAPTER ──────────────────────────────────────────────────────────

        /// <summary>
        /// Demonstrates the Adapter pattern.
        /// The same <see cref="AirportPaymentService"/> processes a ticket
        /// payment using whichever gateway the caller selects (paypal / stripe /
        /// googlepay).  The service only knows the <c>IPaymentProcessor</c>
        /// interface; the concrete SDK is hidden inside the adapter.
        /// </summary>
        [HttpPost("adapter/pay")]
        public IActionResult PayWithAdapter([FromBody] AdapterPaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                return BadRequest("Customer name is required.");
            if (request.Amount <= 0)
                return BadRequest("Amount must be positive.");

            IPaymentProcessor processor = request.Gateway.ToLower() switch
            {
                "paypal"    => new PayPalAdapter(new PayPalSdk()),
                "stripe"    => new StripeAdapter(new StripeSdk()),
                "googlepay" => new GooglePayAdapter(new GooglePayApi()),
                _           => null!
            };

            if (processor is null)
                return BadRequest($"Unknown gateway '{request.Gateway}'. Use: paypal | stripe | googlepay");

            var service = new AirportPaymentService(processor);
            var result  = service.ProcessTicketPurchase(
                request.Amount,
                request.Currency,
                request.FlightNumber);

            return Ok(new AdapterPaymentResponse(
                Gateway         : processor.GatewayName,
                Success         : result.Success,
                TransactionId   : result.TransactionId,
                Message         : result.Message));
        }

        // ── COMPOSITE ────────────────────────────────────────────────────────

        /// <summary>
        /// Demonstrates the Composite pattern.
        /// Returns the airport restaurant's full menu as a nested JSON tree.
        /// Every node – whether a leaf item or a section – is described by the
        /// same DTO shape, which mirrors the uniform <c>IMenuComponent</c> interface.
        /// </summary>
        [HttpGet("composite/menu")]
        public IActionResult GetRestaurantMenu()
        {
            var root = AirportRestaurantMenuFactory.CreateFullMenu();
            var dto  = ToDto(root);
            return Ok(dto);
        }

        /// <summary>
        /// Returns only the menu items whose price falls within the given range.
        /// Shows how you can traverse the composite tree uniformly.
        /// </summary>
        [HttpGet("composite/menu/filter")]
        public IActionResult FilterMenuByPrice(
            [FromQuery] decimal minPrice = 0,
            [FromQuery] decimal maxPrice = decimal.MaxValue)
        {
            var root  = AirportRestaurantMenuFactory.CreateFullMenu();
            var items = FlattenLeaves(root)
                .Where(i => i.Price >= minPrice && i.Price <= maxPrice)
                .Select(i => new MenuItemDto(i.Name, i.Description, i.Price, i.Category, null))
                .ToList();

            return Ok(new { Count = items.Count, Items = items });
        }

        // ── FAÇADE ────────────────────────────────────────────────────────────

        private static readonly HotelReservationFacade _hotelFacade = new();

        /// <summary>
        /// Demonstrates the Façade pattern.
        /// Books a hotel room in one API call, hiding the entire reservation
        /// subsystem (search, availability, payment, repository, notifications).
        /// </summary>
        [HttpPost("facade/book-room")]
        public IActionResult BookHotelRoom([FromBody] BookRoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.GuestName))
                return BadRequest("Guest name is required.");
            if (request.CheckOut <= request.CheckIn)
                return BadRequest("Check-out must be after check-in.");

            var result = _hotelFacade.BookRoom(new HotelBookingRequest(
                GuestName     : request.GuestName,
                GuestEmail    : request.GuestEmail,
                GuestPhone    : request.GuestPhone,
                RoomTypeCode  : request.RoomTypeCode,
                GuestCount    : request.GuestCount,
                CheckIn       : request.CheckIn,
                CheckOut      : request.CheckOut,
                PaymentMethod : request.PaymentMethod));

            if (!result.Success)
                return Conflict(new { Error = result.ErrorMessage });

            return Ok(result.Confirmation);
        }

        /// <summary>Searches available hotel rooms (Façade demo).</summary>
        [HttpGet("facade/rooms")]
        public IActionResult SearchHotelRooms(
            [FromQuery] DateTime? checkIn    = null,
            [FromQuery] DateTime? checkOut   = null,
            [FromQuery] int       guestCount = 1)
        {
            var ci = checkIn  ?? DateTime.Today.AddDays(1);
            var co = checkOut ?? DateTime.Today.AddDays(3);

            var rooms = _hotelFacade
                .SearchRooms(ci, co, guestCount)
                .Select(r => new {
                    r.TypeName,
                    r.MaxGuests,
                    PricePerNight = r.PricePerNight,
                    r.Description
                });

            return Ok(rooms);
        }

        /// <summary>Cancels a hotel booking (Façade demo).</summary>
        [HttpDelete("facade/cancel/{bookingReference}")]
        public IActionResult CancelHotelBooking(string bookingReference)
        {
            bool ok = _hotelFacade.CancelBooking(bookingReference);
            return ok
                ? Ok(new { Message = $"Booking #{bookingReference} cancelled." })
                : NotFound(new { Error = $"Booking #{bookingReference} not found." });
        }

        // ── FLYWEIGHT ─────────────────────────────────────────────────────────

        [HttpGet("flyweight/resources")]
        public IActionResult GetFlyweightResources()
        {
            var factory = new AirportResourceFactory();
            var assignments = new List<AirportResourceAssignment>
            {
                new("G12", "RO101", DateTime.UtcNow.AddMinutes(25), factory.GetResource("Gate", "Terminal A")),
                new("G13", "RO102", DateTime.UtcNow.AddMinutes(40), factory.GetResource("Gate", "Terminal A")),
                new("RWY-1", "FR220", DateTime.UtcNow.AddMinutes(55), factory.GetResource("RunwaySlot", "North Strip")),
                new("RWY-2", "FR221", DateTime.UtcNow.AddMinutes(70), factory.GetResource("RunwaySlot", "North Strip"))
            };

            var usage = assignments.Select(a => new FlyweightUsageDto(
                a.ContextId,
                a.FlightCode,
                a.Resource.ResourceType,
                a.Resource.Zone,
                a.Resource.RenderUsage(a.ContextId, a.FlightCode, a.SlotUtc)));

            return Ok(new FlyweightDemoResponse(factory.SharedObjectCount, assignments.Count, usage.ToList()));
        }

        // ── DECORATOR ─────────────────────────────────────────────────────────

        [HttpPost("decorator/booking")]
        public IActionResult DecorateBooking([FromBody] DecoratorBookingRequest request)
        {
            if (request.BasePrice <= 0)
                return BadRequest("Base price must be positive.");

            IFlightBooking booking = new BaseFlightBooking(request.FlightNumber, request.BasePrice);
            if (request.AddPriorityBoarding)
                booking = new PriorityBoardingDecorator(booking);
            if (request.AddLoungeAccess)
                booking = new LoungeAccessDecorator(booking);

            return Ok(new DecoratorBookingResponse(booking.Describe(), booking.GetTotalPrice()));
        }

        // ── BRIDGE ────────────────────────────────────────────────────────────

        [HttpGet("bridge/operations")]
        public IActionResult RunBridgeOperations(
            [FromQuery] string airportType = "international",
            [FromQuery] string operation = "landing",
            [FromQuery] string identifier = "RO440")
        {
            IAirportImplementation implementation = airportType.Trim().ToLowerInvariant() switch
            {
                "international" => new InternationalAirportImplementation(),
                "domestic" => new DomesticAirportImplementation(),
                "cargo" => new CargoAirportImplementation(),
                _ => null!
            };

            if (implementation is null)
                return BadRequest("airportType must be: international | domestic | cargo");

            AirportOperation abstraction = operation.Trim().ToLowerInvariant() switch
            {
                "landing" => new LandingOperation(implementation),
                "security" => new SecurityOperation(implementation),
                _ => null!
            };

            if (abstraction is null)
                return BadRequest("operation must be: landing | security");

            return Ok(new BridgeOperationResponse(
                implementation.AirportType,
                operation,
                abstraction.Operate(identifier)));
        }

        // ── PROXY ─────────────────────────────────────────────────────────────

        [HttpGet("proxy/access")]
        public IActionResult AccessSensitiveSystems(
            [FromQuery] string system = "atc",
            [FromQuery] string role = "operator",
            [FromQuery] string query = "active-flights")
        {
            ISensitiveAirportSystem realSystem = system.Trim().ToLowerInvariant() switch
            {
                "atc" => new AirTrafficControlSystem(),
                "securitydb" => new SecurityDatabaseSystem(),
                _ => null!
            };

            if (realSystem is null)
                return BadRequest("system must be: atc | securitydb");

            var proxy = system.Trim().ToLowerInvariant() switch
            {
                "atc" => new SensitiveSystemProxy(realSystem, new[] { "controller", "supervisor" }),
                "securitydb" => new SensitiveSystemProxy(realSystem, new[] { "security", "admin" }),
                _ => null!
            };

            return Ok(new ProxyAccessResponse(system, role, proxy!.Access(role, query)));
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private static MenuItemDto ToDto(IMenuComponent component)
        {
            var children = component.GetChildren()
                                    .Select(ToDto)
                                    .ToList();

            string? category = component is MenuItem mi ? mi.Category : null;

            return new MenuItemDto(
                component.Name,
                component.Description,
                component.GetPrice(),
                category,
                children.Count > 0 ? children : null);
        }

        private static IEnumerable<MenuItem> FlattenLeaves(IMenuComponent component)
        {
            if (component is MenuItem leaf)
            {
                yield return leaf;
                yield break;
            }
            foreach (var child in component.GetChildren())
                foreach (var item in FlattenLeaves(child))
                    yield return item;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REQUEST / RESPONSE MODELS
    // ─────────────────────────────────────────────────────────────────────────

    public sealed record AdapterPaymentRequest(
        string  Gateway,
        string  CustomerName,
        decimal Amount,
        string  Currency     = "MDL",
        string  FlightNumber = "RO000");

    public sealed record AdapterPaymentResponse(
        string Gateway,
        bool   Success,
        string TransactionId,
        string Message);

    public sealed record MenuItemDto(
        string             Name,
        string             Description,
        decimal            TotalPrice,
        string?            Category,
        List<MenuItemDto>? Children);

    public sealed record BookRoomRequest(
        string   GuestName,
        string   GuestEmail,
        string?  GuestPhone,
        string   RoomTypeCode,
        int      GuestCount,
        DateTime CheckIn,
        DateTime CheckOut,
        string   PaymentMethod = "Credit Card");

    public sealed record FlyweightUsageDto(
        string ContextId,
        string FlightCode,
        string ResourceType,
        string Zone,
        string Details);

    public sealed record FlyweightDemoResponse(
        int SharedObjects,
        int TotalAssignments,
        List<FlyweightUsageDto> Usages);

    public sealed record DecoratorBookingRequest(
        string FlightNumber,
        decimal BasePrice,
        bool AddPriorityBoarding,
        bool AddLoungeAccess);

    public sealed record DecoratorBookingResponse(
        string Description,
        decimal TotalPrice);

    public sealed record BridgeOperationResponse(
        string AirportType,
        string Operation,
        string Result);

    public sealed record ProxyAccessResponse(
        string System,
        string Role,
        string Result);
}
