
using FOODGOBACKEND.Dtos.Auth;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires authentication for all user types
    public class DevicesController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public DevicesController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registers or updates a device token for push notifications.
        /// Available to all authenticated users (Customer, Shipper, Restaurant).
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user token.");
            }

            // Check if user exists and is active
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized("User account is not active.");
            }

            // Check if device token already exists for this user
            var existingDevice = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == dto.DeviceToken);

            if (existingDevice != null)
            {
                // Update existing device
                existingDevice.DeviceType = dto.DeviceType;
                existingDevice.DeviceModel = dto.DeviceModel;
                existingDevice.AppVersion = dto.AppVersion;
                existingDevice.UpdatedAt = DateTime.UtcNow;
                existingDevice.IsActive = true;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Device token updated successfully." });
            }

            // Create new device registration
            var userDevice = new UserDevice
            {
                UserId = userId,
                DeviceToken = dto.DeviceToken,
                DeviceType = dto.DeviceType,
                DeviceModel = dto.DeviceModel,
                AppVersion = dto.AppVersion,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.UserDevices.AddAsync(userDevice);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Device registered successfully." });
        }

        /// <summary>
        /// Unregisters a device token (e.g., when user logs out).
        /// </summary>
        [HttpPost("unregister")]
        public async Task<IActionResult> UnregisterDevice([FromBody] string deviceToken)
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                return BadRequest("Device token is required.");
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user token.");
            }

            var device = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == deviceToken);

            if (device == null)
            {
                return NotFound("Device not found.");
            }

            device.IsActive = false;
            device.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Device unregistered successfully." });
        }

        /// <summary>
        /// Gets all registered devices for the current user.
        /// </summary>
        [HttpGet("my-devices")]
        public async Task<IActionResult> GetMyDevices()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user token.");
            }

            var devices = await _context.UserDevices
                .Where(d => d.UserId == userId && d.IsActive)
                .Select(d => new
                {
                    d.DeviceId,
                    d.DeviceToken,
                    d.DeviceType,
                    d.DeviceModel,
                    d.AppVersion,
                    d.CreatedAt,
                    d.UpdatedAt
                })
                .ToListAsync();

            return Ok(devices);
        }
    }
}
