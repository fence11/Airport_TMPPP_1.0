using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Airport_TMPPP_1._0.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightQueryService _flightQueryService;
        private readonly IFlightCommandService _flightCommandService;

        public FlightsController(IFlightQueryService flightQueryService, IFlightCommandService flightCommandService)
        {
            _flightQueryService = flightQueryService;
            _flightCommandService = flightCommandService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFlights()
        {
            var flights = await _flightQueryService.GetFlightsAsync();
            return Ok(flights);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFlight(int id)
        {
            var flight = await _flightQueryService.GetFlightByIdAsync(id);
            if (flight == null)
                return NotFound();
            return Ok(flight);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFlight([FromBody] CreateFlightRequest request)
        {
            try
            {
                var flight = await _flightCommandService.CreateFlightAsync(request.FlightNumber, request.AirportId);
                return CreatedAtAction(nameof(GetFlight), new { id = flight.Id }, flight);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFlight(int id, [FromBody] UpdateFlightRequest request)
        {
            try
            {
                var flight = await _flightCommandService.UpdateFlightAsync(id, request.FlightNumber, request.AirportId);
                return Ok(flight);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlight(int id)
        {
            var deleted = await _flightCommandService.DeleteFlightAsync(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }
    }

    public class CreateFlightRequest
    {
        public string FlightNumber { get; set; } = string.Empty;
        public int AirportId { get; set; }
    }

    public class UpdateFlightRequest
    {
        public string FlightNumber { get; set; } = string.Empty;
        public int AirportId { get; set; }
    }
}
