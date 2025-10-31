using FOODGOBACKEND.Dtos.Order;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/restaurant/orders")]
    [ApiController]
    [Authorize(Roles = "RESTAURANT")]
    public class RestaurantOrdersController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public RestaurantOrdersController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all orders for the authenticated restaurant with pagination.
        /// Restaurant Use Case R-UC02: View list of orders.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <param name="status">Optional filter by order status</param>
        [HttpGet]
        public async Task<ActionResult<object>> GetRestaurantOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            var restaurantId = await GetUserRestaurantId();
            if (restaurantId == null)
            {
                return Forbid("User is not associated with a restaurant.");
            }

            var query = _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c.CustomerNavigation)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .Where(o => o.RestaurantId == restaurantId.Value)
                .AsQueryable();

            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderCode,
                    o.OrderStatus,
                    o.DeliveryAddress,
                    o.TotalAmount,
                    o.CreatedAt,
                    CustomerName = o.Customer.FullName,
                    CustomerPhone = o.Customer.CustomerNavigation.PhoneNumber,
                    ItemCount = o.OrderItems.Count,
                    TotalQuantity = o.OrderItems.Sum(oi => oi.Quantity)
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
        /// Gets detailed information about a specific order.
        /// Restaurant Use Case R-UC03: View order details.
        /// </summary>
        /// <param name="id">Order ID</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDetailsDto>> GetOrderDetails(int id)
        {
            var restaurantId = await GetUserRestaurantId();
            if (restaurantId == null)
            {
                return Forbid("User is not associated with a restaurant.");
            }

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
                .Where(o => o.OrderId == id && o.RestaurantId == restaurantId.Value)
                .Select(o => new OrderDetailsDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    OrderStatus = o.OrderStatus,
                    DeliveryAddress = o.DeliveryAddress,
                    Note = o.Note,
                    Subtotal = o.Subtotal,
                    ShippingFee = o.ShippingFee,
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.CreatedAt,
                    ConfirmedAt = o.ConfirmedAt,
                    PreparedAt = o.PreparedAt,
                    DeliveringAt = o.DeliveringAt,
                    CompletedAt = o.CompletedAt,
                    CancelledAt = o.CancelledAt,

                    Customer = new OrderCustomerInfo
                    {
                        CustomerId = o.Customer.CustomerId,
                        FullName = o.Customer.FullName,
                        Email = o.Customer.Email,
                        PhoneNumber = o.Customer.CustomerNavigation.PhoneNumber
                    },

                    Restaurant = new OrderRestaurantInfo
                    {
                        RestaurantId = o.Restaurant.RestaurantId,
                        RestaurantName = o.Restaurant.RestaurantName,
                        Address = o.Restaurant.Address,
                        PhoneNumber = o.Restaurant.PhoneNumber
                    },

                    Shipper = o.Shipper != null ? new OrderShipperInfo
                    {
                        ShipperId = o.Shipper.ShipperId,
                        FullName = o.Shipper.FullName,
                        PhoneNumber = o.Shipper.ShipperNavigation.PhoneNumber,
                        LicensePlate = o.Shipper.LicensePlate
                    } : null,

                    Items = o.OrderItems.Select(oi => new OrderItemDetailsDto
                    {
                        OrderItemId = oi.OrderItemId,
                        DishId = oi.DishId,
                        DishName = oi.Dish.DishName,
                        Description = oi.Dish.Description,
                        ImageUrl = oi.Dish.ImageUrl,
                        Quantity = oi.Quantity,
                        PriceAtOrder = oi.PriceAtOrder,
                        ItemTotal = oi.Quantity * oi.PriceAtOrder
                    }).ToList(),

                    AppliedVouchers = o.OrderVouchers.Select(ov => new OrderVoucherDetailsDto
                    {
                        VoucherCode = ov.Voucher.VoucherCode,
                        Description = ov.Voucher.Description,
                        DiscountApplied = ov.DiscountApplied
                    }).ToList(),

                    Payment = o.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault() != null
                        ? new OrderPaymentInfo
                        {
                            PaymentId = o.Payments.OrderByDescending(p => p.CreatedAt).First().PaymentId,
                            PaymentMethod = o.Payments.OrderByDescending(p => p.CreatedAt).First().PaymentMethod,
                            Amount = o.Payments.OrderByDescending(p => p.CreatedAt).First().Amount,
                            PaymentStatus = o.Payments.OrderByDescending(p => p.CreatedAt).First().PaymentStatus,
                            TransactionCode = o.Payments.OrderByDescending(p => p.CreatedAt).First().TransactionCode,
                            CreatedAt = o.Payments.OrderByDescending(p => p.CreatedAt).First().CreatedAt
                        }
                        : null
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound("Order not found or does not belong to your restaurant.");
            }

            return Ok(order);
        }

        /// <summary>
        /// Updates the status of an order.
        /// Restaurant Use Case R-UC04: Update order status (Confirmed, Prepared, Cancelled).
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="dto">New status information</param>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Normalize status to uppercase
            var normalizedStatus = dto.OrderStatus.ToUpper();

            // Validate status is allowed for restaurant
            if (!OrderStatusConstants.RestaurantAllowedStatuses.Contains(normalizedStatus))
            {
                return BadRequest($"Invalid status. Restaurant can only set: {string.Join(", ", OrderStatusConstants.RestaurantAllowedStatuses)}");
            }

            var restaurantId = await GetUserRestaurantId();
            if (restaurantId == null)
            {
                return Forbid("User is not associated with a restaurant.");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.RestaurantId == restaurantId.Value);

            if (order == null)
            {
                return NotFound("Order not found or does not belong to your restaurant.");
            }

            // Validate status transition
            var validationResult = ValidateStatusTransition(order.OrderStatus, normalizedStatus);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ErrorMessage);
            }

            // Update status and corresponding timestamp
            order.OrderStatus = normalizedStatus;

            switch (normalizedStatus)
            {
                case OrderStatusConstants.Confirmed:
                    order.ConfirmedAt = DateTime.UtcNow;
                    break;
                case OrderStatusConstants.Prepared:
                    order.PreparedAt = DateTime.UtcNow;
                    break;
                case OrderStatusConstants.Cancelled:
                    order.CancelledAt = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Helper method to get restaurant ID for the authenticated user.
        /// </summary>
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

        /// <summary>
        /// Validates if the status transition is allowed.
        /// </summary>
        private (bool IsValid, string? ErrorMessage) ValidateStatusTransition(string currentStatus, string newStatus)
        {
            // Define valid transitions
            var validTransitions = new Dictionary<string, string[]>
            {
                { OrderStatusConstants.Pending, new[] { OrderStatusConstants.Confirmed, OrderStatusConstants.Cancelled } },
                { OrderStatusConstants.Confirmed, new[] { OrderStatusConstants.Prepared, OrderStatusConstants.Cancelled } },
                { OrderStatusConstants.Prepared, new[] { OrderStatusConstants.Cancelled } } // Can still cancel before pickup
            };

            if (!validTransitions.ContainsKey(currentStatus))
            {
                return (false, $"Cannot change status from '{currentStatus}'.");
            }

            if (!validTransitions[currentStatus].Contains(newStatus))
            {
                return (false, $"Cannot change status from '{currentStatus}' to '{newStatus}'. Valid transitions: {string.Join(", ", validTransitions[currentStatus])}");
            }

            return (true, null);
        }
    }
}