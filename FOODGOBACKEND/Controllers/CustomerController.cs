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
        /// Creates a new order for the authenticated customer.
        /// Customer Use Case C-UC05: Create order.
        /// Uses customer's default address for delivery.
        /// Default payment method: CASH.
        /// </summary>
        [HttpPost("orders")]
        public async Task<ActionResult<ResponseOrderDto>> CreateOrder([FromBody] RequestOrderDto dto)
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

            // Verify customer exists
            var customer = await _context.Customers
                .Include(c => c.CustomerNavigation)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            // Get customer's default address
            var defaultAddress = await _context.Addresses
                .Where(a => a.CustomerId == customerId && a.IsDefault)
                .FirstOrDefaultAsync();

            if (defaultAddress == null)
            {
                return BadRequest("Customer must have a default delivery address. Please add an address first.");
            }

            // Verify restaurant exists and is active
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.RestaurantId == dto.RestaurantId && r.IsActive);

            if (restaurant == null)
            {
                return NotFound("Restaurant not found or is not active.");
            }

            // Verify all dishes exist, are available, and belong to the restaurant
            var dishIds = dto.Items.Select(i => i.DishId).Distinct().ToList();
            var dishes = await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId) 
                         && d.RestaurantId == dto.RestaurantId 
                         && d.IsAvailable)
                .ToListAsync();

            if (dishes.Count != dishIds.Count)
            {
                return BadRequest("One or more dishes are not available or do not belong to this restaurant.");
            }

            // Calculate subtotal
            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                var dish = dishes.First(d => d.DishId == item.DishId);
                var itemTotal = dish.Price * item.Quantity;
                subtotal += itemTotal;

                orderItems.Add(new OrderItem
                {
                    DishId = item.DishId,
                    Quantity = item.Quantity,
                    PriceAtOrder = dish.Price
                });
            }

            // ===== TEMPORARILY DISABLED: Distance-based shipping fee calculation =====
            // TODO: Re-enable when geocoding performance is optimized
            // try
            // {
            //     var distance = await GeoLocationHelper.CalculateDistanceBetweenAddressesSimple(
            //         restaurant.Address,
            //         defaultAddress.FullAddress
            //     );
    //
            //     if (distance.HasValue)
            //     {
            //         shippingFee = 15000;
            //         if (distance.Value > 3)
            //         {
            //             shippingFee += (decimal)Math.Ceiling(distance.Value - 3) * 5000;
            //         }
            //     }
            //     else
            //     {
            //         shippingFee = 20000;
            //     }
            // }
            // catch
            // {
            //     shippingFee = 20000;
            // }
            
            // Fixed shipping fee (temporarily)
            decimal shippingFee = 20000; // Default 20,000 VND
            // ===== END TEMPORARY CHANGE =====

            // Calculate total amount
            decimal totalAmount = subtotal + shippingFee;

            // Generate unique order code
            var orderCode = $"FDG-{DateTime.UtcNow:yyyyMMddHHmmss}";

            // Create order with default values
            var order = new Order
            {
                CustomerId = customerId,
                RestaurantId = dto.RestaurantId,
                OrderCode = orderCode,
                DeliveryAddress = defaultAddress.FullAddress,
                Note = null, // No note by default
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                OrderStatus = "PENDING",
                CreatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create payment record with default payment method (CASH)
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Amount = totalAmount,
                PaymentMethod = "CASH", // Default payment method
                PaymentStatus = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Prepare response
            var response = new ResponseOrderDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                RestaurantName = restaurant.RestaurantName,
                DeliveryAddress = order.DeliveryAddress,
                Note = order.Note,
                Subtotal = order.Subtotal,
                ShippingFee = order.ShippingFee,
                DiscountAmount = 0, // No discount applied
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentMethod = payment.PaymentMethod,
                PaymentStatus = payment.PaymentStatus,
                CreatedAt = order.CreatedAt ?? DateTime.UtcNow,
                Items = orderItems.Select(oi => new ResponseOrderItemDto
                {
                    DishId = oi.DishId,
                    DishName = dishes.First(d => d.DishId == oi.DishId).DishName,
                    Quantity = oi.Quantity,
                    PriceAtOrder = oi.PriceAtOrder,
                    Total = oi.Quantity * oi.PriceAtOrder
                }).ToList()
            };

            return Created($"/api/Customer/orders/{order.OrderId}", response);
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
                // ===== TEMPORARILY DISABLED: Distance calculation =====
                // TODO: Re-enable when geocoding performance is optimized
                // double distanceKm = 0;
                // 
                // if (!string.IsNullOrEmpty(customerDefaultAddress))
                // {
                //     var distance = await GeoLocationHelper.CalculateDistanceBetweenAddressesSimple(
                //         customerDefaultAddress,
                //         r.Address
                //     );
    //
                //     if (distance.HasValue)
                //     {
                //         distanceKm = distance.Value;
                //     }
                // }
                
                double distanceKm = 0; // Temporarily set to 0
                // ===== END TEMPORARY CHANGE =====

                result.Add(new ItemRestaurantDto
                {
                    RestaurantId = r.RestaurantId,
                    Name = r.RestaurantName,
                    ImageUrl = null,
                    AverageRating = r.Reviews.Any() ? r.Reviews.Average() : 0,
                    ReviewCount = r.Reviews.Count,
                    CompletedOrderCount = r.CompletedOrders,
                    DistanceInKm = distanceKm
                });
            }

            // Sort by RestaurantId instead of distance (since distance is 0)
            result = result.OrderByDescending(r => r.RestaurantId).ToList();

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
        /// Gets the list of reviews for a specific restaurant.
        /// Customer Use Case: View restaurant reviews.
        /// </summary>
        /// <param name="restaurantId">The ID of the restaurant.</param>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 10).</param>
        /// <returns>Paginated list of reviews for the restaurant.</returns>
        [HttpGet("restaurants/{restaurantId}/reviews")]
        [AllowAnonymous] // Allow unauthenticated users to view reviews
        public async Task<ActionResult<object>> GetRestaurantReviews(
            int restaurantId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // Check if restaurant exists
            var restaurantExists = await _context.Restaurants
                .AnyAsync(r => r.RestaurantId == restaurantId);

            if (!restaurantExists)
            {
                return NotFound("Restaurant not found.");
            }

            // Get all reviews for dishes belonging to this restaurant
            var query = _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Order)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Dish)
                .Where(r => r.OrderItem.Dish.RestaurantId == restaurantId 
                         && r.OrderItem.Order.OrderStatus == "COMPLETED")
                .AsQueryable();

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ItemReviewDto
                {
                    ReviewId = r.ReviewId,
                    UserName = r.Customer.FullName,
                    AvatarUrl = null, // TODO: Add avatar support to User/Customer model
                    ReviewDate = r.CreatedAt.HasValue 
                        ? r.CreatedAt.Value.ToString("dd/MM/yyyy") 
                        : DateTime.Now.ToString("dd/MM/yyyy"),
                    Rating = r.Rating,
                    Content = r.Comment
                })
                .ToListAsync();

            // Calculate statistics
            var allReviews = await _context.Reviews
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Dish)
                .Where(r => r.OrderItem.Dish.RestaurantId == restaurantId)
                .Select(r => r.Rating)
                .ToListAsync();

            var averageRating = allReviews.Any() ? allReviews.Average() : 0;

            return Ok(new
            {
                RestaurantId = restaurantId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                AverageRating = Math.Round(averageRating, 1),
                Data = reviews
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

        /// <summary>
        /// Gets the list of reviews written by the authenticated customer.
        /// Customer Use Case: View my review history.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 10).</param>
        /// <returns>Paginated list of reviews written by the customer.</returns>
        [HttpGet("reviews/my-reviews")]
        public async Task<ActionResult<object>> GetMyReviews(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid customer token.");
            }

            // Get all reviews written by this customer
            var query = _context.Reviews
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Dish)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Dish.Restaurant)
                .Where(r => r.CustomerId == customerId)
                .AsQueryable();

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ItemReviewHistoryDto
                {
                    ReviewId = r.ReviewId,
                    DishName = r.OrderItem.Dish.DishName,
                    RestaurantName = r.OrderItem.Dish.Restaurant.RestaurantName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.CreatedAt.HasValue 
                        ? r.CreatedAt.Value.ToString("dd/MM/yyyy") 
                        : DateTime.Now.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Data = reviews
            });
        }

        /// <summary>
        /// Gets the list of available vouchers for customers.
        /// Customer Use Case: View available vouchers/promotions.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 10).</param>
        /// <returns>Paginated list of available vouchers.</returns>
        [HttpGet("vouchers")]
        [AllowAnonymous] // Allow all users to view available vouchers
        public async Task<ActionResult<object>> GetAvailableVouchers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var now = DateTime.UtcNow;
            
            var query = _context.Vouchers
                .Where(v => v.IsActive == true 
                         && v.ValidFrom <= now 
                         && v.ValidTo >= now
                         && (v.MaxUsage == null || v.CurrentUsage < v.MaxUsage))
                .AsQueryable();

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var vouchers = await query
                .OrderBy(v => v.ValidTo) // Sort by expiration date (closest expiring first)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new ItemVoucherDto
                {
                    VoucherId = v.VoucherId,
                    VoucherCode = v.VoucherCode,
                    Description = v.Description,
                    ValidTo = "HSD: " + v.ValidTo.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Data = vouchers
            });
        }

        /// <summary>
        /// Gets the review screen information for the authenticated customer.
        /// Customer Use Case: Initialize review screen with user info and current date.
        /// </summary>
        /// <returns>User information for the review screen.</returns>
        [HttpGet("reviews/screen-info")]
        public async Task<ActionResult<ResponseReviewScreenDto>> GetScreenReview()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid customer token.");
            }

            // Get customer information
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            var response = new ResponseReviewScreenDto
            {
                UserName = customer.FullName,
                AvatarUrl = null, // TODO: Add avatar support to User/Customer model
                CurrentDate = DateTime.Now.ToString("dd/MM/yyyy")
            };

            return Ok(response);
        }

        /// <summary>
        /// Submits a review for a completed order item.
        /// Customer Use Case: Write review for purchased dish.
        /// </summary>
        /// <param name="dto">Review data to submit.</param>
        /// <returns>Created review with ID.</returns>
        [HttpPost("reviews")]
        public async Task<ActionResult<object>> SubmitOrderReview([FromBody] RequestReviewDto dto)
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

            // Verify the order item exists and belongs to this customer
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Review)
                .Include(oi => oi.Dish)
                    .ThenInclude(d => d.Restaurant)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == dto.OrderItemId);

            if (orderItem == null)
            {
                return NotFound("Order item not found.");
            }

            // Check if the order belongs to this customer
            if (orderItem.Order.CustomerId != customerId)
            {
                return Forbid("You can only review your own orders.");
            }

            // Check if the order is completed
            if (orderItem.Order.OrderStatus != "COMPLETED")
            {
                return BadRequest("You can only review completed orders.");
            }

            // Check if this order item has already been reviewed
            if (orderItem.Review != null)
            {
                return Conflict("This order item has already been reviewed.");
            }

            // Create new review
            var review = new Review
            {
                OrderItemId = dto.OrderItemId,
                CustomerId = customerId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Created($"/api/Customer/reviews/{review.ReviewId}", new
            {
                review.ReviewId,
                review.OrderItemId,
                DishName = orderItem.Dish.DishName,
                RestaurantName = orderItem.Dish.Restaurant.RestaurantName,
                review.Rating,
                review.Comment,
                ReviewDate = review.CreatedAt?.ToString("dd/MM/yyyy"),
                Message = "Review submitted successfully."
            });
        }

        /// <summary>
        /// Gets the profile information for the authenticated customer.
        /// Customer Use Case: View user profile.
        /// </summary>
        /// <returns>Customer profile information.</returns>
        [HttpGet("profile")]
        public async Task<ActionResult<ResponseUserProfileDto>> GetUserProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid customer token.");
            }

            // Get customer information with user navigation
            var customer = await _context.Customers
                .Include(c => c.CustomerNavigation)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            var response = new ResponseUserProfileDto
            {
                FullName = customer.FullName,
                PhoneNumber = customer.CustomerNavigation.PhoneNumber,
                Email = customer.Email,
                AvatarUrl = null // TODO: Add avatar support to User/Customer model
            };

            return Ok(response);
        }

        /// <summary>
        /// Updates the profile information for the authenticated customer.
        /// Customer Use Case: Edit user profile.
        /// </summary>
        /// <param name="dto">Updated profile data.</param>
        /// <returns>Updated profile information.</returns>
        [HttpPut("profile")]
        public async Task<ActionResult<ResponseUserProfileDto>> UpdateUserProfile([FromBody] RequestUserProfileDto dto)
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

            // Get customer with user navigation
            var customer = await _context.Customers
                .Include(c => c.CustomerNavigation)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            // Check if phone number is being changed and if it's already taken
            if (dto.PhoneNumber != customer.CustomerNavigation.PhoneNumber)
            {
                var phoneExists = await _context.Users
                    .AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.UserId != customerId);

                if (phoneExists)
                {
                    return Conflict("Phone number is already in use by another user.");
                }

                // Update phone number in User table
                customer.CustomerNavigation.PhoneNumber = dto.PhoneNumber;
            }

            // Check if email is being changed and if it's already taken
            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != customer.Email)
            {
                var emailExists = await _context.Customers
                    .AnyAsync(c => c.Email == dto.Email && c.CustomerId != customerId);

                if (emailExists)
                {
                    return Conflict("Email is already in use by another customer.");
                }
            }

            // Update customer information
            customer.FullName = dto.FullName;
            customer.Email = dto.Email;
            // TODO: Handle avatar upload/update when avatar feature is implemented
            // customer.AvatarUrl = dto.AvatarUrl;

            await _context.SaveChangesAsync();

            var response = new ResponseUserProfileDto
            {
                FullName = customer.FullName,
                PhoneNumber = customer.CustomerNavigation.PhoneNumber,
                Email = customer.Email,
                AvatarUrl = dto.AvatarUrl // TODO: Return actual avatar URL after implementation
            };

            return Ok(response);
        }

        /// <summary>
        /// Gets detailed information for a specific order.
        /// Customer Use Case: View order details.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <returns>Detailed order information.</returns>
        [HttpGet("orders/{orderId}")]
        public async Task<ActionResult<ResponseOrderDetailDto>> GetOrderDetail(int orderId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid customer token.");
            }

            // Load order with all related data
            var order = await _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c.CustomerNavigation)
                .Include(o => o.Restaurant)
                .Include(o => o.Shipper)
                    .ThenInclude(s => s!.ShipperNavigation)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .Include(o => o.OrderVouchers)
                    .ThenInclude(ov => ov.Voucher)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Verify ownership
            if (order.CustomerId != customerId)
            {
                return Forbid("You can only view your own orders.");
            }

            // Map status to Vietnamese text
            var statusText = order.OrderStatus switch
            {
                "PENDING" => "Đang tìm Tài xế...",
                "CONFIRMED" => "Đã xác nhận",
                "PREPARING" => "Đang chuẩn bị",
                "DELIVERING" => "Đang giao hàng",
                "COMPLETED" => "Đã hoàn thành",
                "CANCELLED" => "Đã hủy",
                _ => "Không xác định"
            };

            // Calculate estimated delivery time using GeoLocationHelper
            string? estimatedDeliveryTime = null;
            
            if (order.OrderStatus == "COMPLETED")
            {
                // Order already completed
                if (order.CompletedAt.HasValue)
                {
                    estimatedDeliveryTime = $"Đã giao lúc {order.CompletedAt.Value:HH:mm}";
                }
            }
            else if (order.OrderStatus == "CANCELLED")
            {
                // Order cancelled
                if (order.CancelledAt.HasValue)
                {
                    estimatedDeliveryTime = $"Đã hủy lúc {order.CancelledAt.Value:HH:mm}";
                }
            }
            else
            {
                // Calculate estimated time for active orders
                var estimatedMinutes = await GeoLocationHelper.CalculateEstimatedDeliveryTime(
                    order.OrderStatus,
                    order.Restaurant.Address,
                    order.DeliveryAddress,
                    (double?)order.Shipper?.CurrentLat,
                    (double?)order.Shipper?.CurrentLng,
                    order.ConfirmedAt,
                    order.PreparedAt,
                    order.DeliveringAt
                );
                
                if (estimatedMinutes > 0)
                {
                    var baseTime = DateTime.Now;
                    var est1 = baseTime.AddMinutes(estimatedMinutes);
                    var est2 = baseTime.AddMinutes(estimatedMinutes + 10); // Add 10 min buffer
                    estimatedDeliveryTime = $"Dự kiến giao lúc {est1:HH:mm} – {est2:HH:mm}";
                }
            }

            // Shipper info (null if not assigned)
            ShipperInfoDto? shipperInfo = null;
            if (order.Shipper != null)
            {
                shipperInfo = new ShipperInfoDto
                {
                    FullName = order.Shipper.FullName,
                    PhoneNumber = order.Shipper.ShipperNavigation.PhoneNumber
                };
            }

            // Address info
            var addressInfo = new AddressInfoDto
            {
                RestaurantName = order.Restaurant.RestaurantName,
                RestaurantAddress = order.Restaurant.Address,
                DeliveryAddress = order.DeliveryAddress,
                CustomerName = order.Customer.FullName,
                CustomerPhone = order.Customer.CustomerNavigation.PhoneNumber
            };

            // Order items
            var items = order.OrderItems.Select(oi => new OrderItemDetailDto
            {
                DishName = oi.Dish.DishName,
                Quantity = oi.Quantity,
                PriceAtOrder = oi.PriceAtOrder
            }).ToList();

            // Calculate discount amount
            var discountAmount = order.OrderVouchers.Sum(ov => ov.DiscountApplied);

            // Calculate service fee
            var serviceFee = order.TotalAmount - order.Subtotal - order.ShippingFee + discountAmount;
            if (serviceFee < 0) serviceFee = 0;

            // Payment status
            var paymentStatus = order.Payments.Any() 
                ? order.Payments.First().PaymentStatus ?? "PENDING"
                : "PENDING";

            var summary = new OrderSummaryDto
            {
                Items = items,
                Subtotal = order.Subtotal,
                ShippingFee = order.ShippingFee,
                ServiceFee = serviceFee,
                DiscountAmount = discountAmount,
                GrandTotal = order.TotalAmount,
                PaymentStatusText = paymentStatus
            };

            var response = new ResponseOrderDetailDto
            {
                StatusText = statusText,
                EstimatedDeliveryTime = estimatedDeliveryTime,
                OrderStatusKey = order.OrderStatus,
                OrderCode = order.OrderCode,
                Note = order.Note,
                ShipperInfo = shipperInfo,
                AddressInfo = addressInfo,
                Summary = summary
            };

            return Ok(response);
        }
    }
}
