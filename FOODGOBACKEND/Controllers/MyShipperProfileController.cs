using FOODGOBACKEND.Dtos.Shipper;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/shipper/profile")]
    [ApiController]
    [Authorize(Roles = "SHIPPER")]
    public class MyShipperProfileController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public MyShipperProfileController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the profile information of the authenticated shipper.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ShipperProfileDto>> GetMyProfile()
        {
            // Get shipper ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid user token.");
            }

            var profile = await _context.Shippers
                .Include(s => s.ShipperNavigation)
                .Where(s => s.ShipperId == shipperId)
                .Select(s => new ShipperProfileDto
                {
                    ShipperId = s.ShipperId,
                    FullName = s.FullName,
                    PhoneNumber = s.ShipperNavigation.PhoneNumber,
                    LicensePlate = s.LicensePlate,
                    IsAvailable = s.IsAvailable,
                    CurrentLat = s.CurrentLat,
                    CurrentLng = s.CurrentLng,
                    IsActive = s.ShipperNavigation.IsActive,
                    CreatedAt = s.ShipperNavigation.CreatedAt,
                    UpdatedAt = s.ShipperNavigation.UpdatedAt,
                    TotalDeliveries = s.Orders.Count,
                    ActiveDeliveries = s.Orders.Count(o => o.OrderStatus == "DELIVERING"),
                    CompletedDeliveries = s.Orders.Count(o => o.OrderStatus == "COMPLETED")
                })
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound("Shipper profile not found.");
            }

            return Ok(profile);
        }

        /// <summary>
        /// Updates the profile information of the authenticated shipper.
        /// Allows partial updates (only non-null fields will be updated).
        /// </summary>
        /// <param name="dto">Profile update data</param>
        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateShipperProfileDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get shipper ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid user token.");
            }

            var shipper = await _context.Shippers
                .Include(s => s.ShipperNavigation)
                .FirstOrDefaultAsync(s => s.ShipperId == shipperId);

            if (shipper == null)
            {
                return NotFound("Shipper profile not found.");
            }

            // Check if account is active
            if (!shipper.ShipperNavigation.IsActive)
            {
                return BadRequest("Account is not active.");
            }

            // Update only non-null properties
            bool hasChanges = false;

            if (dto.FullName != null && dto.FullName != shipper.FullName)
            {
                shipper.FullName = dto.FullName;
                hasChanges = true;
            }

            if (dto.LicensePlate != null && dto.LicensePlate != shipper.LicensePlate)
            {
                shipper.LicensePlate = dto.LicensePlate;
                hasChanges = true;
            }

            if (dto.IsAvailable.HasValue && dto.IsAvailable.Value != shipper.IsAvailable)
            {
                shipper.IsAvailable = dto.IsAvailable.Value;
                hasChanges = true;
            }

            if (dto.CurrentLat.HasValue && dto.CurrentLat.Value != shipper.CurrentLat)
            {
                shipper.CurrentLat = dto.CurrentLat.Value;
                hasChanges = true;
            }

            if (dto.CurrentLng.HasValue && dto.CurrentLng.Value != shipper.CurrentLng)
            {
                shipper.CurrentLng = dto.CurrentLng.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                shipper.ShipperNavigation.UpdatedAt = DateTime.UtcNow;
                _context.Shippers.Update(shipper);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        /// <summary>
        /// Updates only the location of the authenticated shipper.
        /// This is a lightweight endpoint for frequent location updates during deliveries.
        /// </summary>
        /// <param name="dto">Location update data</param>
        [HttpPatch("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateShipperLocationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get shipper ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid user token.");
            }

            var shipper = await _context.Shippers.FindAsync(shipperId);
            if (shipper == null)
            {
                return NotFound("Shipper profile not found.");
            }

            // Update location
            shipper.CurrentLat = dto.Latitude;
            shipper.CurrentLng = dto.Longitude;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Toggles the availability status of the authenticated shipper.
        /// Useful for starting/ending shift.
        /// </summary>
        /// <param name="dto">Availability status</param>
        [HttpPatch("availability")]
        public async Task<IActionResult> UpdateAvailability([FromBody] UpdateShipperAvailabilityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get shipper ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid user token.");
            }

            var shipper = await _context.Shippers
                .Include(s => s.ShipperNavigation)
                .FirstOrDefaultAsync(s => s.ShipperId == shipperId);

            if (shipper == null)
            {
                return NotFound("Shipper profile not found.");
            }

            // Check if account is active
            if (!shipper.ShipperNavigation.IsActive)
            {
                return BadRequest("Account is not active.");
            }

            // Check if shipper has active deliveries
            if (!dto.IsAvailable)
            {
                var hasActiveDeliveries = await _context.Orders
                    .AnyAsync(o => o.ShipperId == shipperId && o.OrderStatus == "DELIVERING");

                if (hasActiveDeliveries)
                {
                    return BadRequest("Cannot set availability to false while you have active deliveries.");
                }
            }

            shipper.IsAvailable = dto.IsAvailable;
            shipper.ShipperNavigation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    /// <summary>
    /// DTO for updating shipper location.
    /// </summary>
    public class UpdateShipperLocationDto
    {
        /// <summary>
        /// Latitude coordinate.
        /// </summary>
        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal Latitude { get; set; }

        /// <summary>
        /// Longitude coordinate.
        /// </summary>
        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal Longitude { get; set; }
    }

    /// <summary>
    /// DTO for updating shipper availability.
    /// </summary>
    public class UpdateShipperAvailabilityDto
    {
        /// <summary>
        /// Availability status.
        /// </summary>
        [Required(ErrorMessage = "Availability status is required.")]
        public bool IsAvailable { get; set; }
    }
}