using Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.FactoryMethod;
using Microsoft.AspNetCore.Mvc;

namespace Airport_TMPPP_1._0.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        /// <summary>
        /// Creates a transport reservation using the Factory Method pattern.
        /// The server selects the concrete transport (Airplane, Train, Bus) via the factory
        /// and returns a summary with the calculated price.
        /// </summary>
        [HttpPost]
        public IActionResult CreateReservation([FromBody] CreateReservationRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            try
            {
                var reservation = TransportReservationFactory.CreateReservation(
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
                    TransportDescription = summary.TransportDescription
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class CreateReservationRequest
    {
        /// <summary>0 = Airplane, 1 = Train, 2 = Bus</summary>
        public int TransportType { get; set; }
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
    }
}
