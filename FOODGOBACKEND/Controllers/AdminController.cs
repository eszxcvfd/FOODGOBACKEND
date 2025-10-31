using FOODGOBACKEND.Dtos.User;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN")] // Accept different case variations
    public class AdminController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public AdminController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a paginated list of all users.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var usersQuery = _context.Users
                .Include(u => u.Customer)
                .Include(u => u.Restaurant)
                .Include(u => u.Shipper)
                .AsQueryable();

            var users = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    PhoneNumber = u.PhoneNumber,
                    UserType = u.UserType,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    FullName = u.Customer != null ? u.Customer.FullName : (u.Shipper != null ? u.Shipper.FullName : null),
                    Email = u.Customer != null ? u.Customer.Email : null,
                    RestaurantName = u.Restaurant != null ? u.Restaurant.RestaurantName : null
                })
                .ToListAsync();

            var totalRecords = await usersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            return Ok(new {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Data = users
            });
        }

        /// <summary>
        /// Gets the details of a specific user by their ID.
        /// </summary>
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Include(u => u.Customer)
                .Include(u => u.Restaurant)
                .Include(u => u.Shipper)
                .Where(u => u.UserId == id)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    PhoneNumber = u.PhoneNumber,
                    UserType = u.UserType,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    FullName = u.Customer != null ? u.Customer.FullName : (u.Shipper != null ? u.Shipper.FullName : null),
                    Email = u.Customer != null ? u.Customer.Email : null,
                    RestaurantName = u.Restaurant != null ? u.Restaurant.RestaurantName : null
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user);
        }

        /// <summary>
        /// Updates the active status of a user (locks or unlocks an account).
        /// </summary>
        [HttpPatch("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusDto updateStatusDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            user.IsActive = updateStatusDto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return NoContent(); // Indicates success with no content to return
        }
    }
}