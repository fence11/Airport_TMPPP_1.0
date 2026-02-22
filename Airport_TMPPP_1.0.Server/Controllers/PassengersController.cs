using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Airport_TMPPP_1._0.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PassengersController : ControllerBase
    {
        private readonly IPassengerQueryService _passengerQueryService;
        private readonly IPassengerCommandService _passengerCommandService;

        public PassengersController(IPassengerQueryService passengerQueryService, IPassengerCommandService passengerCommandService)
        {
            _passengerQueryService = passengerQueryService;
            _passengerCommandService = passengerCommandService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPassengers()
        {
            var passengers = await _passengerQueryService.GetPassengersAsync();
            return Ok(passengers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPassenger(int id)
        {
            var passenger = await _passengerQueryService.GetPassengerByIdAsync(id);
            if (passenger == null)
                return NotFound();
            return Ok(passenger);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePassenger([FromBody] CreatePassengerRequest request)
        {
            try
            {
                var passenger = await _passengerCommandService.CreatePassengerAsync(
                    request.FirstName, 
                    request.LastName, 
                    request.Email, 
                    request.PhoneNumber);
                return CreatedAtAction(nameof(GetPassenger), new { id = passenger.Id }, passenger);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePassenger(int id, [FromBody] UpdatePassengerRequest request)
        {
            try
            {
                var passenger = await _passengerCommandService.UpdatePassengerAsync(id, request.PhoneNumber);
                return Ok(passenger);
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
        public async Task<IActionResult> DeletePassenger(int id)
        {
            var deleted = await _passengerCommandService.DeletePassengerAsync(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }
    }

    public class CreatePassengerRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class UpdatePassengerRequest
    {
        public string? PhoneNumber { get; set; }
    }
}
