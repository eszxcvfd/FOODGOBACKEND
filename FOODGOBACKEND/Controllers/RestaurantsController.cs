using FOODGOBACKEND.Dtos.Dish;
using FOODGOBACKEND.Dtos.Restaurant;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantsController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public RestaurantsController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a paginated list of active restaurants.
        /// Customer Use Case C-UC03: View list of restaurants.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <param name="searchTerm">Optional search term to filter by restaurant name or address</param>
        [HttpGet]
        [AllowAnonymous] // Customers can view without authentication
        public async Task<ActionResult<object>> GetRestaurants(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            var query = _context.Restaurants
                .Include(r => r.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Review)
                .Where(r => r.IsActive)
                .AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => 
                    r.RestaurantName.Contains(searchTerm) || 
                    r.Address.Contains(searchTerm));
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var restaurants = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RestaurantSummaryDto
                {
                    RestaurantId = r.RestaurantId,
                    RestaurantName = r.RestaurantName,
                    Address = r.Address,
                    PhoneNumber = r.PhoneNumber,
                    OpeningTime = r.OpeningTime,
                    ClosingTime = r.ClosingTime,
                    IsActive = r.IsActive,
                    
                    // Calculate average rating from reviews
                    AverageRating = r.Orders
                        .SelectMany(o => o.OrderItems)
                        .Where(oi => oi.Review != null)
                        .Average(oi => (decimal?)oi.Review!.Rating),
                    
                    // Count total reviews
                    TotalReviews = r.Orders
                        .SelectMany(o => o.OrderItems)
                        .Count(oi => oi.Review != null)
                })
                .ToListAsync();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Data = restaurants
            });
        }

        /// <summary>
        /// Gets detailed information about a specific restaurant including its menu.
        /// Customer Use Case C-UC04: View restaurant details and menu.
        /// </summary>
        /// <param name="id">The restaurant ID</param>
        [HttpGet("{id}")]
        [AllowAnonymous] // Customers can view without authentication
        public async Task<ActionResult<RestaurantDetailDto>> GetRestaurantDetails(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.Dishes)
                .Include(r => r.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Review)
                .Where(r => r.RestaurantId == id && r.IsActive)
                .Select(r => new RestaurantDetailDto
                {
                    RestaurantId = r.RestaurantId,
                    RestaurantName = r.RestaurantName,
                    Address = r.Address,
                    PhoneNumber = r.PhoneNumber,
                    OpeningTime = r.OpeningTime,
                    ClosingTime = r.ClosingTime,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    
                    // Calculate statistics
                    AverageRating = r.Orders
                        .SelectMany(o => o.OrderItems)
                        .Where(oi => oi.Review != null)
                        .Average(oi => (decimal?)oi.Review!.Rating),
                    
                    TotalReviews = r.Orders
                        .SelectMany(o => o.OrderItems)
                        .Count(oi => oi.Review != null),
                    
                    TotalOrders = r.Orders.Count(o => o.OrderStatus == "COMPLETED"),
                    
                    // Get available dishes (menu)
                    Dishes = r.Dishes
                        .Where(d => d.IsAvailable)
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
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (restaurant == null)
            {
                return NotFound("Restaurant not found or not active.");
            }

            return Ok(restaurant);
        }
    }
}