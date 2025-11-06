using FOODGOBACKEND.Dtos.Test;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FOODGOBACKEND.Controllers
{
    /// <summary>
    /// Test controller for seeding/pushing data to database.
    /// Only for development/testing purposes.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // For testing - remove in production
    public class TestController : ControllerBase
    {
        private readonly FoodGoContext _context;
        private readonly IWebHostEnvironment _environment;

        public TestController(FoodGoContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        /// <summary>
        /// Pushes a new restaurant to the database.
        /// POST: api/Test/restaurants
        /// </summary>
        /// <param name="dto">Restaurant data to push.</param>
        /// <returns>Created restaurant with ID.</returns>
        [HttpPost("restaurants")]
        public async Task<ActionResult<object>> PushRestaurant([FromBody] PushRestaurantDto dto)
        {
            // Validate owner exists and is RESTAURANT role
            var owner = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.OwnerId);

            if (owner == null)
            {
                return BadRequest($"Owner with ID {dto.OwnerId} does not exist.");
            }

            // Sửa từ "RESTAURANT_OWNER" thành "RESTAURANT"
            if (owner.UserType != "RESTAURANT")
            {
                return BadRequest($"User {dto.OwnerId} is not a RESTAURANT owner. Current type: {owner.UserType}");
            }

            // Check if owner already has a restaurant
            var existingRestaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerId == dto.OwnerId);

            if (existingRestaurant != null)
            {
                return Conflict($"Owner {dto.OwnerId} already has a restaurant (ID: {existingRestaurant.RestaurantId}).");
            }

            // Parse time strings
            TimeOnly? openingTime = null;
            TimeOnly? closingTime = null;

            if (!string.IsNullOrEmpty(dto.OpeningTime))
            {
                if (!TimeOnly.TryParse(dto.OpeningTime, out var parsedOpeningTime))
                {
                    return BadRequest("Invalid OpeningTime format. Use HH:mm (e.g., 08:00).");
                }
                openingTime = parsedOpeningTime;
            }

            if (!string.IsNullOrEmpty(dto.ClosingTime))
            {
                if (!TimeOnly.TryParse(dto.ClosingTime, out var parsedClosingTime))
                {
                    return BadRequest("Invalid ClosingTime format. Use HH:mm (e.g., 22:00).");
                }
                closingTime = parsedClosingTime;
            }

            // Create new restaurant
            var restaurant = new Restaurant
            {
                OwnerId = dto.OwnerId,
                RestaurantName = dto.RestaurantName,
                Address = dto.Address,
                PhoneNumber = dto.PhoneNumber,
                OpeningTime = openingTime,
                ClosingTime = closingTime,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            return Created($"/api/Test/restaurants/{restaurant.RestaurantId}", new
            {
                restaurant.RestaurantId,
                restaurant.OwnerId,
                restaurant.RestaurantName,
                restaurant.Address,
                restaurant.PhoneNumber,
                OpeningTime = restaurant.OpeningTime?.ToString("HH:mm"),
                ClosingTime = restaurant.ClosingTime?.ToString("HH:mm"),
                restaurant.IsActive,
                restaurant.CreatedAt
            });
        }

        /// <summary>
        /// Pushes a new dish to a restaurant with image upload.
        /// POST: api/Test/dishes
        /// </summary>
        /// <param name="dto">Dish data to push (multipart/form-data).</param>
        /// <returns>Created dish with ID.</returns>
        [HttpPost("dishes")]
        public async Task<ActionResult<object>> PushDishByRestaurant([FromForm] PushDishDto dto)
        {
            // Validate restaurant exists
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.RestaurantId == dto.RestaurantId);

            if (restaurant == null)
            {
                return NotFound($"Restaurant with ID {dto.RestaurantId} does not exist.");
            }

            // Validate price
            if (dto.Price <= 0)
            {
                return BadRequest("Price must be greater than 0.");
            }

            // Handle image upload
            string? imageFileName = null;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(dto.ImageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Invalid file type. Only .jpg, .jpeg, .png, .gif, .webp are allowed.");
                }

                // Validate file size (max 5MB)
                if (dto.ImageFile.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("File size must not exceed 5MB.");
                }

                // FIX: Check if WebRootPath is null
                var webRootPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(webRootPath))
                {
                    // Fallback: Use ContentRootPath if WebRootPath is null
                    webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
                }

                // Create Img directory if it doesn't exist
                var imgDirectory = Path.Combine(webRootPath, "Img");
                if (!Directory.Exists(imgDirectory))
                {
                    Directory.CreateDirectory(imgDirectory);
                }

                // Generate unique filename
                imageFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(imgDirectory, imageFileName);

                // Save file to disk
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.ImageFile.CopyToAsync(stream);
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error saving file: {ex.Message}");
                }
            }

            // Create new dish
            var dish = new Dish
            {
                RestaurantId = dto.RestaurantId,
                DishName = dto.DishName,
                Description = dto.Description,
                Price = dto.Price,
                ImageUrl = imageFileName, // Store only filename
                IsAvailable = dto.IsAvailable
            };

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();

            // Generate full image URL for response
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullImageUrl = imageFileName != null ? $"{baseUrl}/Img/{imageFileName}" : null;

            return Created($"/api/Test/dishes/{dish.DishId}", new
            {
                dish.DishId,
                dish.RestaurantId,
                RestaurantName = restaurant.RestaurantName,
                dish.DishName,
                dish.Description,
                dish.Price,
                ImageFileName = dish.ImageUrl,
                ImageUrl = fullImageUrl,
                dish.IsAvailable
            });
        }

        /// <summary>
        /// Gets all restaurants (for testing).
        /// GET: api/Test/restaurants
        /// </summary>
        [HttpGet("restaurants")]
        public async Task<ActionResult<object>> GetAllRestaurants()
        {
            var restaurants = await _context.Restaurants
                .Include(r => r.Owner)
                .Select(r => new
                {
                    r.RestaurantId,
                    r.OwnerId,
                    OwnerPhone = r.Owner.PhoneNumber,
                    r.RestaurantName,
                    r.Address,
                    r.PhoneNumber,
                    OpeningTime = r.OpeningTime != null ? r.OpeningTime.Value.ToString("HH:mm") : null,
                    ClosingTime = r.ClosingTime != null ? r.ClosingTime.Value.ToString("HH:mm") : null,
                    r.IsActive,
                    r.CreatedAt,
                    DishCount = r.Dishes.Count
                })
                .ToListAsync();

            return Ok(new
            {
                TotalRecords = restaurants.Count,
                Data = restaurants
            });
        }

        /// <summary>
        /// Gets all dishes by restaurant (for testing).
        /// GET: api/Test/restaurants/{restaurantId}/dishes
        /// </summary>
        [HttpGet("restaurants/{restaurantId}/dishes")]
        public async Task<ActionResult<object>> GetDishesByRestaurant(int restaurantId)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.RestaurantId == restaurantId);

            if (restaurant == null)
            {
                return NotFound($"Restaurant with ID {restaurantId} does not exist.");
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var dishes = await _context.Dishes
                .Where(d => d.RestaurantId == restaurantId)
                .Select(d => new
                {
                    d.DishId,
                    d.RestaurantId,
                    d.DishName,
                    d.Description,
                    d.Price,
                    ImageFileName = d.ImageUrl,
                    ImageUrl = d.ImageUrl != null ? $"{baseUrl}/Img/{d.ImageUrl}" : null,
                    d.IsAvailable
                })
                .ToListAsync();

            return Ok(new
            {
                RestaurantId = restaurantId,
                RestaurantName = restaurant.RestaurantName,
                TotalDishes = dishes.Count,
                Data = dishes
            });
        }
    }
}