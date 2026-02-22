using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Airport_TMPPP_1._0.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AirportsController : ControllerBase
    {
        private readonly IAirportQueryService _airportQueryService;
        private readonly IAirportCommandService _airportCommandService;

        public AirportsController(IAirportQueryService airportQueryService, IAirportCommandService airportCommandService)
        {
            _airportQueryService = airportQueryService;
            _airportCommandService = airportCommandService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAirports()
        {
            var airports = await _airportQueryService.GetAirportsAsync();
            return Ok(airports);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAirport(int id)
        {
            var airport = await _airportQueryService.GetAirportByIdAsync(id);
            if (airport == null)
                return NotFound();
            return Ok(airport);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAirport([FromBody] CreateAirportRequest request)
        {
            try
            {
                var airport = await _airportCommandService.CreateAirportAsync(request.Name, request.Code);
                return CreatedAtAction(nameof(GetAirport), new { id = airport.Id }, airport);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAirport(int id, [FromBody] UpdateAirportRequest request)
        {
            try
            {
                var airport = await _airportCommandService.UpdateAirportAsync(id, request.Name, request.Code);
                return Ok(airport);
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
        public async Task<IActionResult> DeleteAirport(int id)
        {
            try
            {
                var deleted = await _airportCommandService.DeleteAirportAsync(id);
                if (!deleted)
                    return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }

    public class CreateAirportRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class UpdateAirportRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
