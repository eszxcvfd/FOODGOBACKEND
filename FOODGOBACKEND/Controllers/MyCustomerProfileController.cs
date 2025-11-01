using FOODGOBACKEND.Dtos.Customer;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/customer/profile")]
    [ApiController]
    [Authorize(Roles = "CUSTOMER")]
    public class MyCustomerProfileController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public MyCustomerProfileController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the profile information of the authenticated customer.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<CustomerProfileDto>> GetMyProfile()
        {
            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            var profile = await _context.Customers
                .Include(c => c.CustomerNavigation)
                .Where(c => c.CustomerId == customerId)
                .Select(c => new CustomerProfileDto
                {
                    CustomerId = c.CustomerId,
                    FullName = c.FullName,
                    Email = c.Email,
                    PhoneNumber = c.CustomerNavigation.PhoneNumber,
                    IsActive = c.CustomerNavigation.IsActive,
                    CreatedAt = c.CustomerNavigation.CreatedAt,
                    UpdatedAt = c.CustomerNavigation.UpdatedAt,
                    TotalOrders = c.Orders.Count,
                    TotalReviews = c.Reviews.Count,
                    TotalAddresses = c.Addresses.Count
                })
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound("Customer profile not found.");
            }

            return Ok(profile);
        }

        /// <summary>
        /// Updates the profile information of the authenticated customer.
        /// Allows partial updates (only non-null fields will be updated).
        /// </summary>
        /// <param name="dto">Profile update data</param>
        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateCustomerProfileDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            var customer = await _context.Customers
                .Include(c => c.CustomerNavigation)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return NotFound("Customer profile not found.");
            }

            // Check if account is active
            if (!customer.CustomerNavigation.IsActive)
            {
                return BadRequest("Account is not active.");
            }

            // Update only non-null properties
            bool hasChanges = false;

            if (dto.FullName != null && dto.FullName != customer.FullName)
            {
                customer.FullName = dto.FullName;
                hasChanges = true;
            }

            if (dto.Email != null && dto.Email != customer.Email)
            {
                // Check if email is already in use by another customer
                var emailExists = await _context.Customers
                    .AnyAsync(c => c.Email == dto.Email && c.CustomerId != customerId);

                if (emailExists)
                {
                    return BadRequest("Email is already in use by another account.");
                }

                customer.Email = dto.Email;
                hasChanges = true;
            }

            if (hasChanges)
            {
                customer.CustomerNavigation.UpdatedAt = DateTime.UtcNow;
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}