using FOODGOBACKEND.Dtos.Order;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/shipper/orders")]
    [ApiController]
    [Authorize(Roles = "SHIPPER")]
    public class ShipperOrdersController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public ShipperOrdersController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all orders assigned to the authenticated shipper.
        /// Shipper Use Case S-UC02: View list of assigned orders.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <param name="status">Optional filter by order status</param>
        [HttpGet]
        public async Task<ActionResult<object>> GetAssignedOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            var shipperId = await GetUserShipperId();
            if (shipperId == null)
            {
                return Forbid("User is not associated with a shipper account.");
            }

            var query = _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c.CustomerNavigation)
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .Where(o => o.ShipperId == shipperId.Value)
                .AsQueryable();

            // Apply status filter if provided (normalize to uppercase)
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
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderCode,
                    o.OrderStatus,
                    o.DeliveryAddress,
                    o.TotalAmount,
                    o.Note,
                    o.CreatedAt,
                    o.PreparedAt,
                    o.DeliveringAt,
                    CustomerName = o.Customer.FullName,
                    CustomerPhone = o.Customer.CustomerNavigation.PhoneNumber,
                    RestaurantName = o.Restaurant.RestaurantName,
                    RestaurantAddress = o.Restaurant.Address,
                    RestaurantPhone = o.Restaurant.PhoneNumber,
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
        /// Gets available orders that are ready for pickup (status = PREPARED).
        /// Shipper Use Case S-UC01: View available orders for pickup.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        [HttpGet("available")]
        public async Task<ActionResult<object>> GetAvailableOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c.CustomerNavigation)
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                .Where(o => o.OrderStatus == OrderStatusConstants.Prepared && o.ShipperId == null)
                .AsQueryable();

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var orders = await query
                .OrderBy(o => o.PreparedAt) // Oldest prepared orders first
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderCode,
                    o.DeliveryAddress,
                    o.TotalAmount,
                    o.ShippingFee,
                    o.Note,
                    o.PreparedAt,
                    CustomerName = o.Customer.FullName,
                    CustomerPhone = o.Customer.CustomerNavigation.PhoneNumber,
                    RestaurantName = o.Restaurant.RestaurantName,
                    RestaurantAddress = o.Restaurant.Address,
                    RestaurantPhone = o.Restaurant.PhoneNumber,
                    ItemCount = o.OrderItems.Count
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
        /// Assigns an available order to the authenticated shipper.
        /// Shipper Use Case S-UC01: Accept an order for delivery.
        /// </summary>
        /// <param name="orderId">Order ID to accept</param>
        [HttpPost("{orderId}/accept")]
        public async Task<IActionResult> AcceptOrder(int orderId)
        {
            var shipperId = await GetUserShipperId();
            if (shipperId == null)
            {
                return Forbid("User is not associated with a shipper account.");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId 
                                       && o.OrderStatus == OrderStatusConstants.Prepared 
                                       && o.ShipperId == null);

            if (order == null)
            {
                return NotFound("Order not found or not available for pickup.");
            }

            // Check if shipper is available
            var shipper = await _context.Shippers.FindAsync(shipperId.Value);
            if (shipper == null || !shipper.IsAvailable)
            {
                return BadRequest("Shipper is not available.");
            }

            // Assign order to shipper
            order.ShipperId = shipperId.Value;

            // Optionally set shipper as unavailable
            shipper.IsAvailable = false;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order accepted successfully.", OrderId = orderId });
        }

        /// <summary>
        /// Gets detailed information about a specific assigned order.
        /// Shipper Use Case S-UC03: View order details.
        /// </summary>
        /// <param name="id">Order ID</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDetailsDto>> GetOrderDetails(int id)
        {
            var shipperId = await GetUserShipperId();
            if (shipperId == null)
            {
                return Forbid("User is not associated with a shipper account.");
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
                .Where(o => o.OrderId == id && o.ShipperId == shipperId.Value)
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
                return NotFound("Order not found or not assigned to you.");
            }

            return Ok(order);
        }

        /// <summary>
        /// Updates the delivery status of an order.
        /// Shipper Use Case S-UC05: Update delivery status (Delivering, Completed).
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="dto">New status information</param>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateDeliveryStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Normalize status to uppercase
            var normalizedStatus = dto.OrderStatus.ToUpper();

            // Validate status is allowed for shipper
            if (!OrderStatusConstants.ShipperAllowedStatuses.Contains(normalizedStatus))
            {
                return BadRequest($"Invalid status. Shipper can only set: {string.Join(", ", OrderStatusConstants.ShipperAllowedStatuses)}");
            }

            var shipperId = await GetUserShipperId();
            if (shipperId == null)
            {
                return Forbid("User is not associated with a shipper account.");
            }

            var order = await _context.Orders
                .Include(o => o.Shipper)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.ShipperId == shipperId.Value);

            if (order == null)
            {
                return NotFound("Order not found or not assigned to you.");
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
                case OrderStatusConstants.Delivering:
                    order.DeliveringAt = DateTime.UtcNow;
                    break;
                case OrderStatusConstants.Completed:
                    order.CompletedAt = DateTime.UtcNow;
                    // Set shipper back to available
                    if (order.Shipper != null)
                    {
                        order.Shipper.IsAvailable = true;
                    }
                    break;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Helper method to get shipper ID for the authenticated user.
        /// </summary>
        private async Task<int?> GetUserShipperId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            var shipper = await _context.Shippers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShipperId == userId);

            return shipper?.ShipperId;
        }

        /// <summary>
        /// Validates if the status transition is allowed for shipper.
        /// </summary>
        private (bool IsValid, string? ErrorMessage) ValidateStatusTransition(string currentStatus, string newStatus)
        {
            // Define valid transitions for shipper
            var validTransitions = new Dictionary<string, string[]>
            {
                { OrderStatusConstants.Prepared, new[] { OrderStatusConstants.Delivering } },
                { OrderStatusConstants.Delivering, new[] { OrderStatusConstants.Completed } }
            };

            if (!validTransitions.ContainsKey(currentStatus))
            {
                return (false, $"Cannot change status from '{currentStatus}' as a shipper.");
            }

            if (!validTransitions[currentStatus].Contains(newStatus))
            {
                return (false, $"Cannot change status from '{currentStatus}' to '{newStatus}'. Valid transitions: {string.Join(", ", validTransitions[currentStatus])}");
            }

            return (true, null);
        }
    }
}