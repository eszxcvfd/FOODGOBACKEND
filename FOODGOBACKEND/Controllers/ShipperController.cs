using FOODGOBACKEND.Dtos.Shipper;
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
    [Authorize(Roles = "SHIPPER")]
    public class ShipperController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public ShipperController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the current assigned order for the authenticated shipper.
        /// The system automatically assigns orders to the 3 nearest available shippers.
        /// Each shipper can only see one order at a time until they accept or the order is reassigned.
        /// Shipper Use Case S-UC01: View assigned order.
        /// </summary>
        /// <returns>The currently assigned order for this shipper, or null if no order is assigned.</returns>
        [HttpGet("order")]
        public async Task<ActionResult<ItemOrderCardDto?>> GetOrder()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid shipper token.");
            }

            // Check if shipper exists and is available
            var shipper = await _context.Shippers
                .FirstOrDefaultAsync(s => s.ShipperId == shipperId);

            if (shipper == null)
            {
                return NotFound("Shipper not found.");
            }

            // If shipper is not available (on break), return no orders
            if (!shipper.IsAvailable)
            {
                return Ok(new
                {
                    Message = "You are currently on break. No orders will be assigned.",
                    Data = (ItemOrderCardDto?)null
                });
            }

            // Find the current order assigned to this shipper that is in PENDING status
            // PENDING means the order is waiting for shipper acceptance
            var assignedOrder = await _context.Orders
                .Include(o => o.Restaurant)
                .Where(o => o.ShipperId == shipperId && o.OrderStatus == "PENDING")
                .OrderBy(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (assignedOrder != null)
            {
                // Calculate shipper income (assuming 30% of shipping fee)
                var shipperIncome = assignedOrder.ShippingFee * 0.7m;

                var orderDto = new ItemOrderCardDto
                {
                    OrderId = assignedOrder.OrderId,
                    OrderCode = assignedOrder.OrderCode,
                    StatusText = "CHỜ NHẬN ĐON",
                    RestaurantName = assignedOrder.Restaurant.RestaurantName,
                    RestaurantAddress = assignedOrder.Restaurant.Address,
                    TotalPrice = assignedOrder.TotalAmount,
                    Income = shipperIncome
                };

                return Ok(new
                {
                    Message = "You have a pending order to accept.",
                    Data = orderDto
                });
            }

            // No order currently assigned to this shipper
            // Check if there are any PENDING orders without shipper assignment
            await TryAssignOrdersToNearestShippers();

            // Check again if an order was just assigned
            assignedOrder = await _context.Orders
                .Include(o => o.Restaurant)
                .Where(o => o.ShipperId == shipperId && o.OrderStatus == "PENDING")
                .OrderBy(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (assignedOrder != null)
            {
                var shipperIncome = assignedOrder.ShippingFee * 0.7m;

                var orderDto = new ItemOrderCardDto
                {
                    OrderId = assignedOrder.OrderId,
                    OrderCode = assignedOrder.OrderCode,
                    StatusText = "CHỜ NHẬN ĐON",
                    RestaurantName = assignedOrder.Restaurant.RestaurantName,
                    RestaurantAddress = assignedOrder.Restaurant.Address,
                    TotalPrice = assignedOrder.TotalAmount,
                    Income = shipperIncome
                };

                return Ok(new
                {
                    Message = "New order assigned!",
                    Data = orderDto
                });
            }

            return Ok(new
            {
                Message = "No orders available at the moment.",
                Data = (ItemOrderCardDto?)null
            });
        }

        /// <summary>
        /// Gets the list of available orders near the shipper's current location for free pick.
        /// Shipper can browse and choose which order to deliver.
        /// Shipper Use Case S-UC06: Browse available orders for free pick.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 10).</param>
        /// <param name="maxDistanceKm">Maximum distance in kilometers to filter orders (default: 10km).</param>
        /// <returns>List of available orders sorted by distance.</returns>
        [HttpGet("orders/free-pick")]
        public async Task<ActionResult<object>> GetOrderFreePick(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] double maxDistanceKm = 10.0)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid shipper token.");
            }

            // Get shipper information with location
            var shipper = await _context.Shippers
                .FirstOrDefaultAsync(s => s.ShipperId == shipperId);

            if (shipper == null)
            {
                return NotFound("Shipper not found.");
            }

            // Check if shipper is available
            if (!shipper.IsAvailable)
            {
                return Ok(new
                {
                    Message = "You are currently on break. Turn on availability to see orders.",
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = 0,
                    TotalRecords = 0,
                    Data = new List<ItemFoodFreePickDto>()
                });
            }

            // Check if shipper has location data
            if (shipper.CurrentLat == null || shipper.CurrentLng == null)
            {
                return BadRequest("Please update your location first to see available orders.");
            }

            // Check if shipper already has a pending or confirmed order
            var hasActiveOrder = await _context.Orders
                .AnyAsync(o => o.ShipperId == shipperId 
                            && (o.OrderStatus == "PENDING" 
                             || o.OrderStatus == "CONFIRMED" 
                             || o.OrderStatus == "PREPARING" 
                             || o.OrderStatus == "DELIVERING"));

            if (hasActiveOrder)
            {
                return BadRequest("You already have an active order. Please complete it first before picking a new one.");
            }

            // Get all PENDING orders without shipper assignment
            var pendingOrders = await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .Where(o => o.ShipperId == null && o.OrderStatus == "PENDING")
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            if (!pendingOrders.Any())
            {
                return Ok(new
                {
                    Message = "No available orders at the moment.",
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = 0,
                    TotalRecords = 0,
                    Data = new List<ItemFoodFreePickDto>()
                });
            }

            // Calculate distance for each order and filter by max distance
            var ordersWithDistance = new List<(Order order, double distanceKm)>();

            foreach (var order in pendingOrders)
            {
                try
                {
                    var distance = await GeoLocationHelper.CalculateDistanceBetweenAddressesSimple(
                        $"{shipper.CurrentLat},{shipper.CurrentLng}",
                        order.Restaurant.Address
                    );

                    if (distance.HasValue && distance.Value <= maxDistanceKm)
                    {
                        ordersWithDistance.Add((order, distance.Value));
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other orders
                    Console.WriteLine($"Error calculating distance for order {order.OrderId}: {ex.Message}");
                    continue;
                }
            }

            // Sort by distance (nearest first)
            ordersWithDistance = ordersWithDistance.OrderBy(x => x.distanceKm).ToList();

            var totalRecords = ordersWithDistance.Count;
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // Apply pagination
            var paginatedOrders = ordersWithDistance
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Map to DTO
            var result = new List<ItemFoodFreePickDto>();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            foreach (var (order, distanceKm) in paginatedOrders)
            {
                // Calculate shipper income (70% of shipping fee)
                var shipperIncome = order.ShippingFee * 0.7m;

                // Generate main dish summary (first item)
                var firstItem = order.OrderItems.FirstOrDefault();
                var mainDish = firstItem != null 
                    ? $"{firstItem.Quantity}x {firstItem.Dish.DishName}"
                    : "Không có món";

                // Generate more items text
                var remainingItemsCount = order.OrderItems.Count - 1;
                var moreItems = remainingItemsCount > 0 
                    ? $"+ {remainingItemsCount} món khác" 
                    : null;

                // Format distance
                var distanceText = $"Cách bạn {distanceKm:F1} km";

                // Format destination
                var destination = $"Giao tới: {order.DeliveryAddress}";

                // Format total price
                var totalPrice = $"Tổng: {order.TotalAmount:N0}đ";

                // Format income
                var income = $"Thu nhập: {shipperIncome:N0}đ";

                // TODO: Add restaurant logo support in the future
                // For now, we can use a placeholder or null
                string? shopLogoUrl = null;

                result.Add(new ItemFoodFreePickDto
                {
                    OrderId = order.OrderId,
                    ShopLogoUrl = shopLogoUrl,
                    ShopName = order.Restaurant.RestaurantName,
                    Distance = distanceText,
                    MainDish = mainDish,
                    MoreItems = moreItems,
                    Destination = destination,
                    TotalPrice = totalPrice,
                    Income = income
                });
            }

            return Ok(new
            {
                Message = $"Found {totalRecords} available orders within {maxDistanceKm}km.",
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                MaxDistanceKm = maxDistanceKm,
                ShipperLocation = new
                {
                    Latitude = shipper.CurrentLat,
                    Longitude = shipper.CurrentLng
                },
                Data = result
            });
        }

        /// <summary>
        /// Picks/claims an available order from the free pick list.
        /// The order will be assigned to the shipper and status changed to CONFIRMED.
        /// Shipper Use Case S-UC07: Pick/claim an available order.
        /// </summary>
        /// <param name="orderId">The ID of the order to pick.</param>
        [HttpPost("orders/{orderId}/pick")]
        public async Task<ActionResult<object>> PickOrder(int orderId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid shipper token.");
            }

            var shipper = await _context.Shippers
                .FirstOrDefaultAsync(s => s.ShipperId == shipperId);

            if (shipper == null)
            {
                return NotFound("Shipper not found.");
            }

            // Check if shipper is available
            if (!shipper.IsAvailable)
            {
                return BadRequest("You are currently on break. Turn on availability to pick orders.");
            }

            // Check if shipper already has an active order
            var hasActiveOrder = await _context.Orders
                .AnyAsync(o => o.ShipperId == shipperId 
                            && (o.OrderStatus == "PENDING" 
                             || o.OrderStatus == "CONFIRMED" 
                             || o.OrderStatus == "PREPARING" 
                             || o.OrderStatus == "DELIVERING"));

            if (hasActiveOrder)
            {
                return BadRequest("You already have an active order. Please complete it first before picking a new one.");
            }

            // Get the order
            var order = await _context.Orders
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Check if order is still available
            if (order.OrderStatus != "PENDING")
            {
                return BadRequest($"Order is no longer available. Current status: {order.OrderStatus}");
            }

            if (order.ShipperId != null)
            {
                return BadRequest("This order has already been picked by another shipper.");
            }

            // Assign the order to the shipper
            order.ShipperId = shipperId;
            order.OrderStatus = "CONFIRMED";
            order.ConfirmedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Order picked successfully! You can now proceed to the restaurant.",
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                RestaurantName = order.Restaurant.RestaurantName,
                RestaurantAddress = order.Restaurant.Address,
                Status = order.OrderStatus
            });
        }

        /// <summary>
        /// Background process to assign pending orders to the 3 nearest available shippers.
        /// This method is called automatically when a shipper checks for orders.
        /// </summary>
        private async Task TryAssignOrdersToNearestShippers()
        {
            // Find all PENDING orders that don't have a shipper assigned yet
            // (ShipperId is null and OrderStatus is PENDING)
            var pendingOrders = await _context.Orders
                .Include(o => o.Restaurant)
                .Where(o => o.ShipperId == null && o.OrderStatus == "PENDING")
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            if (!pendingOrders.Any())
            {
                return; // No orders to assign
            }

            // Get all available shippers who:
            // 1. Are marked as available (IsAvailable = true)
            // 2. Have location data (CurrentLat and CurrentLng are not null)
            // 3. Don't have any pending orders assigned to them already
            var availableShippers = await _context.Shippers
                .Where(s => s.IsAvailable 
                         && s.CurrentLat != null 
                         && s.CurrentLng != null
                         && !s.Orders.Any(o => o.OrderStatus == "PENDING"))
                .ToListAsync();

            if (!availableShippers.Any())
            {
                return; // No available shippers
            }

            foreach (var order in pendingOrders)
            {
                try
                {
                    // Calculate distance from restaurant to each available shipper
                    var shipperDistances = new List<(Shipper shipper, double distance)>();

                    foreach (var shipper in availableShippers)
                    {
                        var distance = await GeoLocationHelper.CalculateDistanceBetweenAddressesSimple(
                            order.Restaurant.Address,
                            $"{shipper.CurrentLat},{shipper.CurrentLng}" // Use shipper's current coordinates
                        );

                        if (distance.HasValue)
                        {
                            shipperDistances.Add((shipper, distance.Value));
                        }
                    }

                    if (!shipperDistances.Any())
                    {
                        continue; // Skip this order if no distances could be calculated
                    }

                    // Get the 3 nearest shippers
                    var nearestShippers = shipperDistances
                        .OrderBy(sd => sd.distance)
                        .Take(3)
                        .Select(sd => sd.shipper)
                        .ToList();

                    if (!nearestShippers.Any())
                    {
                        continue;
                    }

                    // Assign the order to the nearest available shipper
                    // (In the future, you can implement a notification system to all 3 shippers)
                    var assignedShipper = nearestShippers.First();
                    order.ShipperId = assignedShipper.ShipperId;

                    // Remove this shipper from available list for next orders
                    availableShippers.Remove(assignedShipper);

                    // TODO: Send push notification to the assigned shipper
                    // await NotificationService.SendOrderNotification(assignedShipper.ShipperId, order.OrderId);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other orders
                    Console.WriteLine($"Error assigning order {order.OrderId}: {ex.Message}");
                    continue;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates the shipper's availability status.
        /// When a shipper goes on break (IsAvailable = false), their pending orders will be reassigned.
        /// Shipper Use Case S-UC02: Toggle availability status.
        /// </summary>
        /// <param name="isAvailable">True if shipper is available to receive orders, false if on break.</param>
        [HttpPut("availability")]
        public async Task<ActionResult<object>> UpdateAvailability([FromBody] bool isAvailable)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid shipper token.");
            }

            var shipper = await _context.Shippers
                .FirstOrDefaultAsync(s => s.ShipperId == shipperId);

            if (shipper == null)
            {
                return NotFound("Shipper not found.");
            }

            shipper.IsAvailable = isAvailable;

            // If shipper goes on break, reassign their pending orders
            if (!isAvailable)
            {
                var pendingOrders = await _context.Orders
                    .Where(o => o.ShipperId == shipperId && o.OrderStatus == "PENDING")
                    .ToListAsync();

                foreach (var order in pendingOrders)
                {
                    // Unassign the order so it can be picked up by another shipper
                    order.ShipperId = null;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = isAvailable 
                    ? "You are now available to receive orders." 
                    : "You are now on break. Pending orders have been reassigned.",
                IsAvailable = shipper.IsAvailable
            });
        }

        /// <summary>
        /// Updates the shipper's current location.
        /// This is used for calculating distance when assigning orders.
        /// Shipper Use Case S-UC03: Update location.
        /// </summary>
        /// <param name="latitude">Current latitude.</param>
        /// <param name="longitude">Current longitude.</param>
        [HttpPut("location")]
        public async Task<ActionResult<object>> UpdateLocation(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid shipper token.");
            }

            var shipper = await _context.Shippers
                .FirstOrDefaultAsync(s => s.ShipperId == shipperId);

            if (shipper == null)
            {
                return NotFound("Shipper not found.");
            }

            shipper.CurrentLat = latitude;
            shipper.CurrentLng = longitude;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Location updated successfully.",
                CurrentLat = shipper.CurrentLat,
                CurrentLng = shipper.CurrentLng
            });
        }

        /// <summary>
        /// Accepts the currently assigned order and changes its status to CONFIRMED.
        /// Shipper Use Case S-UC04: Accept order.
        /// </summary>
        /// <param name="orderId">The ID of the order to accept.</param>
        [HttpPost("order/{orderId}/accept")]
        public async Task<ActionResult<object>> AcceptOrder(int orderId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid shipper token.");
            }

            var order = await _context.Orders
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Verify the order is assigned to this shipper
            if (order.ShipperId != shipperId)
            {
                return Forbid("This order is not assigned to you.");
            }

            // Verify order is in PENDING status
            if (order.OrderStatus != "PENDING")
            {
                return BadRequest($"Order is already {order.OrderStatus}.");
            }

            // Accept the order
            order.OrderStatus = "CONFIRMED";
            order.ConfirmedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Order accepted successfully.",
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                Status = order.OrderStatus
            });
        }

        /// <summary>
        /// Rejects the currently assigned order.
        /// The order will be reassigned to another available shipper.
        /// Shipper Use Case S-UC05: Reject order.
        /// </summary>
        /// <param name="orderId">The ID of the order to reject.</param>
        [HttpPost("order/{orderId}/reject")]
        public async Task<ActionResult<object>> RejectOrder(int orderId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var shipperId))
            {
                return Unauthorized("Invalid shipper token.");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Verify the order is assigned to this shipper
            if (order.ShipperId != shipperId)
            {
                return Forbid("This order is not assigned to you.");
            }

            // Verify order is in PENDING status
            if (order.OrderStatus != "PENDING")
            {
                return BadRequest($"Order is already {order.OrderStatus}.");
            }

            // Unassign the order so it can be reassigned to another shipper
            order.ShipperId = null;

            await _context.SaveChangesAsync();

            // Try to assign to another shipper immediately
            await TryAssignOrdersToNearestShippers();

            return Ok(new
            {
                Message = "Order rejected. It will be reassigned to another shipper.",
                OrderId = order.OrderId
            });
        }
    }
}