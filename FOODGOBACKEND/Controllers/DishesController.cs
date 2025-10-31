using FOODGOBACKEND.Dtos.Dish;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "RESTAURANT")]
    public class DishesController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public DishesController(FoodGoContext context)
        {
            _context = context;
        }

        // POST: api/Dishes
        /// <summary>
        /// Adds a new dish to the restaurant's menu.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DishDto>> AddDish([FromBody] CreateDishDto createDishDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var restaurantId = await GetUserRestaurantId();
            if (restaurantId == null)
            {
                return Forbid("User is not associated with a restaurant.");
            }

            var dish = new Dish
            {
                RestaurantId = restaurantId.Value,
                DishName = createDishDto.DishName,
                Description = createDishDto.Description,
                Price = createDishDto.Price,
                ImageUrl = createDishDto.ImageUrl,
                IsAvailable = createDishDto.IsAvailable
            };

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();

            var dishDto = new DishDto
            {
                DishId = dish.DishId,
                RestaurantId = dish.RestaurantId,
                DishName = dish.DishName,
                Description = dish.Description,
                Price = dish.Price,
                ImageUrl = dish.ImageUrl,
                IsAvailable = dish.IsAvailable
            };

            return CreatedAtAction(nameof(GetDishById), new { id = dish.DishId }, dishDto);
        }

        // PATCH: api/Dishes/{id}
        /// <summary>
        /// Updates the details of an existing dish.
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateDish(int id, [FromBody] UpdateDishDto updateDishDto)
        {
            var restaurantId = await GetUserRestaurantId();
            if (restaurantId == null)
            {
                return Forbid("User is not associated with a restaurant.");
            }

            var dish = await _context.Dishes.FindAsync(id);

            if (dish == null)
            {
                return NotFound("Dish not found.");
            }

            if (dish.RestaurantId != restaurantId)
            {
                return Forbid("You do not have permission to update this dish.");
            }

            if (updateDishDto.DishName != null) dish.DishName = updateDishDto.DishName;
            if (updateDishDto.Description != null) dish.Description = updateDishDto.Description;
            if (updateDishDto.Price.HasValue) dish.Price = updateDishDto.Price.Value;
            if (updateDishDto.ImageUrl != null) dish.ImageUrl = updateDishDto.ImageUrl;
            if (updateDishDto.IsAvailable.HasValue) dish.IsAvailable = updateDishDto.IsAvailable.Value;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Dishes/{id}
        /// <summary>
        /// Deletes a dish from the menu.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDish(int id)
        {
            var restaurantId = await GetUserRestaurantId();
            if (restaurantId == null)
            {
                return Forbid("User is not associated with a restaurant.");
            }

            var dish = await _context.Dishes.FindAsync(id);

            if (dish == null)
            {
                return NotFound("Dish not found.");
            }

            if (dish.RestaurantId != restaurantId)
            {
                return Forbid("You do not have permission to delete this dish.");
            }

            _context.Dishes.Remove(dish);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Dishes/{id}/availability
        /// <summary>
        /// Updates only the availability status of a dish.
        /// </summary>
        [HttpPatch("{id}/availability")]
        public async Task<IActionResult> UpdateAvailability(int id, [FromBody] UpdateDishAvailabilityDto availabilityDto)
        {
            var restaurantId = await GetUserRestaurantId();
            if (restaurantId == null)
            {
                return Forbid("User is not associated with a restaurant.");
            }

            var dish = await _context.Dishes.FindAsync(id);

            if (dish == null)
            {
                return NotFound("Dish not found.");
            }

            if (dish.RestaurantId != restaurantId)
            {
                return Forbid("You do not have permission to update this dish.");
            }

            dish.IsAvailable = availabilityDto.IsAvailable;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Dishes/{id} - Helper endpoint for CreatedAtAction
        [HttpGet("{id}")]
        [AllowAnonymous] // Allow anyone to view a dish by ID
        public async Task<ActionResult<DishDto>> GetDishById(int id)
        {
            var dish = await _context.Dishes
                .Where(d => d.DishId == id)
                .Select(d => new DishDto
                {
                    DishId = d.DishId,
                    RestaurantId = d.RestaurantId,
                    DishName = d.DishName,
                    Description = d.Description,
                    Price = d.Price,
                    ImageUrl = d.ImageUrl,
                    IsAvailable = d.IsAvailable
                })
                .FirstOrDefaultAsync();

            if (dish == null)
            {
                return NotFound();
            }

            return Ok(dish);
        }

        private async Task<int?> GetUserRestaurantId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerId == userId);

            return restaurant?.RestaurantId;
        }
    }
}