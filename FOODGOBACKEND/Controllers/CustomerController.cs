using FOODGOBACKEND.Dtos.Customer;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "CUSTOMER")]
    public class CustomerController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public CustomerController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the list of available dishes for a specific restaurant.
        /// Customer Use Case C-UC03: View restaurant's menu.
        /// </summary>
        /// <param name="restaurantId">The ID of the restaurant.</param>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 10).</param>
        [HttpGet("restaurants/{restaurantId}/dishes")]
        [AllowAnonymous] // Allow even unauthenticated users to browse menus
        public async Task<ActionResult<object>> GetDishesByRestaurant(
            int restaurantId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // Check if the restaurant exists and is active
            var restaurantExists = await _context.Restaurants
                .AnyAsync(r => r.RestaurantId == restaurantId && r.IsActive);

            if (!restaurantExists)
            {
                return NotFound("Restaurant not found or is not active.");
            }

            var query = _context.Dishes
                .Where(d => d.RestaurantId == restaurantId && d.IsAvailable)
                .AsQueryable();

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var dishes = await query
                .OrderByDescending(d => d.DishId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new ItemFoodDto
                {
                    DishId = d.DishId,
                    DishName = d.DishName,
                    ImageUrl = d.ImageUrl,
                    Price = d.Price,
                    // Calculate average rating and count from associated OrderItem.Review
                    AverageRating = d.OrderItems
                        .Where(oi => oi.Review != null)
                        .Any() ? d.OrderItems
                        .Where(oi => oi.Review != null)
                        .Average(oi => oi.Review.Rating) : 0,
                    RatingCount = d.OrderItems
                        .Count(oi => oi.Review != null),
                    // Calculate total sold from completed orders
                    TotalSold = d.OrderItems
                        .Where(oi => oi.Order.OrderStatus == "COMPLETED")
                        .Sum(oi => oi.Quantity)
                })
                .ToListAsync();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Data = dishes
            });
        }

        /// <summary>
        /// Gets the list of all available dishes from active restaurants.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 10).</param>
        [HttpGet("dishes")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetDishes(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Dishes
                .Where(d => d.IsAvailable && d.Restaurant.IsActive)
                .AsQueryable();

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var dishes = await query
                .OrderByDescending(d => d.DishId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new ItemFoodDto
                {
                    DishId = d.DishId,
                    DishName = d.DishName,
                    ImageUrl = d.ImageUrl,
                    Price = d.Price,
                    AverageRating = d.OrderItems
                        .Where(oi => oi.Review != null)
                        .Any() ? d.OrderItems
                        .Where(oi => oi.Review != null)
                        .Average(oi => oi.Review.Rating) : 0,
                    RatingCount = d.OrderItems
                        .Count(oi => oi.Review != null),
                    TotalSold = d.OrderItems
                                 .Where(oi => oi.Order.OrderStatus == "COMPLETED")
                                 .Sum(oi => oi.Quantity)
                })
                .ToListAsync();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Data = dishes
            });
        }
    }
}
