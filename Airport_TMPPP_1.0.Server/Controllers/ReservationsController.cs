using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.FactoryMethod;
using Microsoft.AspNetCore.Mvc;

namespace Airport_TMPPP_1._0.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        // Controllers only know about the factory and the ITransport interface, not the concrete classes
        [HttpPost]
        public IActionResult CreateReservation([FromBody] CreateReservationRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            try
            {
                IReservationFactory factory = request.TicketClass switch
                {
                    TicketClass.Economy => new EconomyReservationFactory(),
                    TicketClass.FirstClass => new FirstClassReservationFactory(),
                    _ => throw new ArgumentOutOfRangeException(nameof(request.TicketClass), request.TicketClass, "Unsupported ticket class.")
                };

                var reservation = factory.CreateReservation(
                    (TransportType)request.TransportType,
                    request.CustomerName,
                    request.DistanceKm);

                var summary = reservation.MakeReservation();

                return Ok(new ReservationResponse
                {
                    CustomerName = summary.CustomerName,
                    TransportName = summary.TransportName,
                    DistanceKm = summary.DistanceKm,
                    Price = summary.Price,
                    TransportDescription = summary.TransportDescription,
                    TicketClass = (TicketClass)request.TicketClass
                });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class CreateReservationRequest
    {
        public int TransportType { get; set; }
        public TicketClass TicketClass { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int DistanceKm { get; set; }
    }

    public class ReservationResponse
    {
        public string CustomerName { get; set; } = string.Empty;
        public string TransportName { get; set; } = string.Empty;
        public int DistanceKm { get; set; }
        public decimal Price { get; set; }
        public string TransportDescription { get; set; } = string.Empty;
        public TicketClass TicketClass { get; set; }
    }
}
