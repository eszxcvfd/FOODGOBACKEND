using FOODGOBACKEND.Dtos.Customer;
using FOODGOBACKEND.Helpers;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        /// Gets the order history for the authenticated customer.
        /// Customer Use Case C-UC06: View order history.
        /// </summary>
        [HttpGet("orders/history")]
        public async Task<ActionResult<object>> GetOrderHistory(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid customer token.");
            }

            // Step 2: Create a database query for orders.
            var query = _context.Orders
                // Step 3: IMPORTANT - Filter the orders to match ONLY the logged-in customer's ID.
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .AsQueryable();

            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.ToUpper();
                query = query.Where(o => o.OrderStatus == normalizedStatus);
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new ItemOrderHistoryDto
                {
                    OrderId = o.OrderId,
                    RestaurantName = o.Restaurant.RestaurantName,
                    OrderStatus = o.OrderStatus,
                    OrderDate = o.CreatedAt ?? DateTime.MinValue,
                    TotalPrice = o.TotalAmount,
                    // Generate a summary of the first few items
                    OrderSummary = string.Join(", ", o.OrderItems
                        .Take(2) // Take first 2 items for a brief summary
                        .Select(oi => $"{oi.Quantity}x {oi.Dish.DishName}"))
                        + (o.OrderItems.Count > 2 ? "..." : "")
                })
                .ToListAsync();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Data = orders
            });
        }


        /// <summary>
        /// Gets the list of addresses for the authenticated customer.
        /// Customer Use Case C-UC04: Manage addresses.
        /// </summary>
        [HttpGet("addresses")]
        public async Task<ActionResult<IEnumerable<ItemAddressDto>>> GetMyAddresses()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid customer token.");
            }

            var addresses = await _context.Addresses
                .Where(a => a.CustomerId == customerId)
                .Include(a => a.Customer)
                    .ThenInclude(c => c.CustomerNavigation)
                .OrderByDescending(a => a.IsDefault) // Show default address first
                .ThenByDescending(a => a.AddressId)
                .Select(a => new ItemAddressDto
                {
                    AddressId = a.AddressId,
                    CustomerName = a.Customer.FullName,
                    CustomerPhone = a.Customer.CustomerNavigation.PhoneNumber,
                    FullAddress = a.FullAddress,
                    IsDefault = a.IsDefault
                })
                .ToListAsync();

            return Ok(addresses);
        }

        /// <summary>
        /// Adds a new address for the authenticated customer.
        /// Customer Use Case C-UC04: Manage addresses.
        /// </summary>
        /// <param name="dto">Address data to add.</param>
        /// <returns>Created address with ID.</returns>
        [HttpPost("addresses")]
        public async Task<ActionResult<object>> AddAddress([FromBody] RequestAddressDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid customer token.");
            }

            // Check if customer exists
            var customerExists = await _context.Customers
                .AnyAsync(c => c.CustomerId == customerId);

            if (!customerExists)
            {
                return NotFound("Customer not found.");
            }

            // If this is set as default, unset all other default addresses
            if (dto.IsDefault)
            {
                var existingAddresses = await _context.Addresses
                    .Where(a => a.CustomerId == customerId && a.IsDefault)
                    .ToListAsync();

                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                }
            }
            else
            {
                // If this is the first address, make it default
                var hasAddresses = await _context.Addresses
                    .AnyAsync(a => a.CustomerId == customerId);

                if (!hasAddresses)
                {
                    dto.IsDefault = true;
                }
            }

            // Build full address if not provided
            var fullAddress = dto.FullAddress;
            if (string.IsNullOrWhiteSpace(fullAddress))
            {
                var parts = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(dto.Street))
                    parts.Add(dto.Street);
                
                if (!string.IsNullOrWhiteSpace(dto.Ward))
                    parts.Add(dto.Ward);
                
                if (!string.IsNullOrWhiteSpace(dto.District))
                    parts.Add(dto.District);
                
                if (!string.IsNullOrWhiteSpace(dto.City))
                    parts.Add(dto.City);

                fullAddress = string.Join(", ", parts);
            }

            // Create new address
            var address = new Address
            {
                CustomerId = customerId,
                Street = dto.Street,
                Ward = dto.Ward,
                District = dto.District,
                City = dto.City,
                FullAddress = fullAddress,
                IsDefault = dto.IsDefault
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            // Get customer info for response
            var customer = await _context.Customers
                .Include(c => c.CustomerNavigation)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            return Created($"/api/Customer/addresses/{address.AddressId}", new
            {
                address.AddressId,
                address.CustomerId,
                CustomerName = customer?.FullName,
                CustomerPhone = customer?.CustomerNavigation.PhoneNumber,
                address.Street,
                address.Ward,
                address.District,
                address.City,
                address.FullAddress,
                address.IsDefault
            });
        }

        /// <summary>
        /// Gets the list of active restaurants with distance calculation from customer's default address.
        /// Customer Use Case C-UC02: Browse restaurants.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 10).</param>
        [HttpGet("restaurants")]
        [AllowAnonymous] // Allow unauthenticated users to browse restaurants
        public async Task<ActionResult<object>> GetRestaurants(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // Get customer's default address if authenticated
            string? customerDefaultAddress = null;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var customerId))
            {
                var defaultAddress = await _context.Addresses
                    .Where(a => a.CustomerId == customerId && a.IsDefault)
                    .Select(a => a.FullAddress)
                    .FirstOrDefaultAsync();

                customerDefaultAddress = defaultAddress;
            }

            var query = _context.Restaurants
                .Where(r => r.IsActive)
                .Include(r => r.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Review)
                .AsQueryable();

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var restaurants = await query
                .OrderByDescending(r => r.RestaurantId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.RestaurantId,
                    r.RestaurantName,
                    r.Address,
                    // Get all reviews from completed orders
                    Reviews = r.Orders
                        .Where(o => o.OrderStatus == "COMPLETED")
                        .SelectMany(o => o.OrderItems)
                        .Where(oi => oi.Review != null)
                        .Select(oi => oi.Review!.Rating)
                        .ToList(),
                    // Count completed orders
                    CompletedOrders = r.Orders
                        .Count(o => o.OrderStatus == "COMPLETED")
                })
                .ToListAsync();

            // Calculate distance for each restaurant
            var result = new List<ItemRestaurantDto>();

            foreach (var r in restaurants)
            {
                double distanceKm = 0;

                // Calculate distance if customer has default address
                if (!string.IsNullOrEmpty(customerDefaultAddress))
                {
                    var distance = await GeoLocationHelper.CalculateDistanceBetweenAddressesSimple(
                        customerDefaultAddress,
                        r.Address
                    );

                    if (distance.HasValue)
                    {
                        distanceKm = distance.Value;
                    }
                }

                result.Add(new ItemRestaurantDto
                {
                    RestaurantId = r.RestaurantId,
                    Name = r.RestaurantName,
                    ImageUrl = null, // TODO: Add restaurant image support
                    AverageRating = r.Reviews.Any() ? r.Reviews.Average() : 0,
                    ReviewCount = r.Reviews.Count,
                    CompletedOrderCount = r.CompletedOrders,
                    DistanceInKm = distanceKm
                });
            }

            // Sort by distance if customer is authenticated and has default address
            if (!string.IsNullOrEmpty(customerDefaultAddress))
            {
                result = result.OrderBy(r => r.DistanceInKm).ToList();
            }

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                HasCustomerAddress = !string.IsNullOrEmpty(customerDefaultAddress),
                CustomerDefaultAddress = customerDefaultAddress,
                Data = result
            });
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

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var dishes = await query
                .OrderByDescending(d => d.DishId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new ItemFoodDto
                {
                    DishId = d.DishId,
                    DishName = d.DishName,
                    ImageUrl = d.ImageUrl != null ? $"{baseUrl}/Img/{d.ImageUrl}" : null,
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

            // Lấy base URL của server
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var dishes = await query
                .OrderByDescending(d => d.DishId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new ItemFoodDto
                {
                    DishId = d.DishId,
                    DishName = d.DishName,
                    // Tạo URL đầy đủ cho ảnh
                    ImageUrl = !string.IsNullOrEmpty(d.ImageUrl) 
                        ? (d.ImageUrl.StartsWith("http") ? d.ImageUrl : $"{baseUrl}/Img/{d.ImageUrl}")
                        : null,
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
