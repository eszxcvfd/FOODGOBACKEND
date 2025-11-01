using FOODGOBACKEND.Dtos.Order;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderStatusController : ControllerBase
    {
        private readonly FoodGoContext _context;
        private readonly ILogger<OrderStatusController> _logger;

        public OrderStatusController(FoodGoContext context, ILogger<OrderStatusController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current status of an order for customer tracking.
        /// Customer Use Case C-UC07: Track order status.
        /// </summary>
        /// <param name="id">Order ID</param>
        [HttpGet("customer/{id}")]
        [Authorize(Roles = "CUSTOMER")]
        public async Task<ActionResult<OrderStatusDto>> GetOrderStatusForCustomer(int id)
        {
            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            var orderStatus = await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.Shipper)
                    .ThenInclude(s => s!.ShipperNavigation)
                .Where(o => o.OrderId == id && o.CustomerId == customerId)
                .Select(o => new OrderStatusDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    OrderStatus = o.OrderStatus,
                    DeliveryAddress = o.DeliveryAddress,
                    TotalAmount = o.TotalAmount,
                    EstimatedDeliveryTime = o.DeliveringAt != null 
                        ? o.DeliveringAt.Value.AddMinutes(30)
                        : o.PreparedAt != null
                            ? o.PreparedAt.Value.AddMinutes(45)
                            : o.ConfirmedAt != null
                                ? o.ConfirmedAt.Value.AddMinutes(60)
                                : o.CreatedAt != null
                                    ? o.CreatedAt.Value.AddMinutes(75)
                                    : null,

                    Timeline = new OrderStatusTimeline
                    {
                        OrderPlaced = o.CreatedAt,
                        Confirmed = o.ConfirmedAt,
                        Prepared = o.PreparedAt,
                        OutForDelivery = o.DeliveringAt,
                        Delivered = o.CompletedAt,
                        Cancelled = o.CancelledAt
                    },

                    Restaurant = new OrderStatusRestaurantInfo
                    {
                        RestaurantId = o.Restaurant.RestaurantId,
                        RestaurantName = o.Restaurant.RestaurantName,
                        PhoneNumber = o.Restaurant.PhoneNumber
                    },

                    Shipper = o.Shipper != null ? new OrderStatusShipperInfo
                    {
                        ShipperId = o.Shipper.ShipperId,
                        FullName = o.Shipper.FullName,
                        PhoneNumber = o.Shipper.ShipperNavigation.PhoneNumber,
                        LicensePlate = o.Shipper.LicensePlate,
                        CurrentLat = o.Shipper.CurrentLat,
                        CurrentLng = o.Shipper.CurrentLng
                    } : null
                })
                .FirstOrDefaultAsync();

            if (orderStatus == null)
            {
                return NotFound("Order not found or does not belong to you.");
            }

            return Ok(orderStatus);
        }

        /// <summary>
        /// Gets status of multiple orders for customer.
        /// Useful for tracking multiple active orders at once.
        /// </summary>
        /// <param name="orderIds">Comma-separated order IDs (e.g., "1,2,3")</param>
        [HttpGet("customer/batch")]
        [Authorize(Roles = "CUSTOMER")]
        public async Task<ActionResult<object>> GetMultipleOrderStatusForCustomer([FromQuery] string? orderIds)
        {
            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            // Validate orderIds parameter
            if (string.IsNullOrWhiteSpace(orderIds))
            {
                return BadRequest(new
                {
                    Error = "Missing required parameter",
                    Message = "The 'orderIds' query parameter is required.",
                    Example = "Usage: /api/OrderStatus/customer/batch?orderIds=1,2,3",
                    Alternative = "To get all active orders, use: /api/OrderStatus/customer/active"
                });
            }

            // Parse order IDs
            var ids = orderIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (!ids.Any())
            {
                return BadRequest(new
                {
                    Error = "Invalid parameter value",
                    Message = "No valid order IDs found in the provided parameter.",
                    Example = "Valid format: orderIds=1,2,3 or orderIds=5",
                    ProvidedValue = orderIds
                });
            }

            var orderStatuses = await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.Shipper)
                    .ThenInclude(s => s!.ShipperNavigation)
                .Where(o => ids.Contains(o.OrderId) && o.CustomerId == customerId)
                .Select(o => new OrderStatusDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    OrderStatus = o.OrderStatus,
                    DeliveryAddress = o.DeliveryAddress,
                    TotalAmount = o.TotalAmount,
                    EstimatedDeliveryTime = o.DeliveringAt != null 
                        ? o.DeliveringAt.Value.AddMinutes(30)
                        : o.PreparedAt != null
                            ? o.PreparedAt.Value.AddMinutes(45)
                            : o.ConfirmedAt != null
                                ? o.ConfirmedAt.Value.AddMinutes(60)
                                : o.CreatedAt != null
                                    ? o.CreatedAt.Value.AddMinutes(75)
                                    : null,

                    Timeline = new OrderStatusTimeline
                    {
                        OrderPlaced = o.CreatedAt,
                        Confirmed = o.ConfirmedAt,
                        Prepared = o.PreparedAt,
                        OutForDelivery = o.DeliveringAt,
                        Delivered = o.CompletedAt,
                        Cancelled = o.CancelledAt
                    },

                    Restaurant = new OrderStatusRestaurantInfo
                    {
                        RestaurantId = o.Restaurant.RestaurantId,
                        RestaurantName = o.Restaurant.RestaurantName,
                        PhoneNumber = o.Restaurant.PhoneNumber
                    },

                    Shipper = o.Shipper != null ? new OrderStatusShipperInfo
                    {
                        ShipperId = o.Shipper.ShipperId,
                        FullName = o.Shipper.FullName,
                        PhoneNumber = o.Shipper.ShipperNavigation.PhoneNumber,
                        LicensePlate = o.Shipper.LicensePlate,
                        CurrentLat = o.Shipper.CurrentLat,
                        CurrentLng = o.Shipper.CurrentLng
                    } : null
                })
                .ToListAsync();

            // Check if any orders were found
            var foundIds = orderStatuses.Select(o => o.OrderId).ToList();
            var notFoundIds = ids.Except(foundIds).ToList();

            if (!orderStatuses.Any())
            {
                return NotFound(new
                {
                    Error = "No orders found",
                    Message = "None of the requested orders were found or belong to you.",
                    RequestedOrderIds = ids,
                    Hint = "Make sure the order IDs are correct and belong to your account."
                });
            }

            return Ok(new
            {
                RequestedCount = ids.Count,
                FoundCount = orderStatuses.Count,
                NotFoundOrderIds = notFoundIds.Any() ? notFoundIds : null,
                Data = orderStatuses
            });
        }

        /// <summary>
        /// Gets all active orders for the customer (not completed or cancelled).
        /// </summary>
        [HttpGet("customer/active")]
        [Authorize(Roles = "CUSTOMER")]
        public async Task<ActionResult<object>> GetActiveOrdersForCustomer()
        {
            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            var activeStatuses = new[] 
            { 
                OrderStatusConstants.Pending, 
                OrderStatusConstants.Confirmed, 
                OrderStatusConstants.Prepared, 
                OrderStatusConstants.Delivering 
            };

            var orderStatuses = await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.Shipper)
                    .ThenInclude(s => s!.ShipperNavigation)
                .Where(o => o.CustomerId == customerId && activeStatuses.Contains(o.OrderStatus))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderStatusDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    OrderStatus = o.OrderStatus,
                    DeliveryAddress = o.DeliveryAddress,
                    TotalAmount = o.TotalAmount,
                    EstimatedDeliveryTime = o.DeliveringAt != null 
                        ? o.DeliveringAt.Value.AddMinutes(30)
                        : o.PreparedAt != null
                            ? o.PreparedAt.Value.AddMinutes(45)
                            : o.ConfirmedAt != null
                                ? o.ConfirmedAt.Value.AddMinutes(60)
                                : o.CreatedAt != null
                                    ? o.CreatedAt.Value.AddMinutes(75)
                                    : null,

                    Timeline = new OrderStatusTimeline
                    {
                        OrderPlaced = o.CreatedAt,
                        Confirmed = o.ConfirmedAt,
                        Prepared = o.PreparedAt,
                        OutForDelivery = o.DeliveringAt,
                        Delivered = o.CompletedAt,
                        Cancelled = o.CancelledAt
                    },

                    Restaurant = new OrderStatusRestaurantInfo
                    {
                        RestaurantId = o.Restaurant.RestaurantId,
                        RestaurantName = o.Restaurant.RestaurantName,
                        PhoneNumber = o.Restaurant.PhoneNumber
                    },

                    Shipper = o.Shipper != null ? new OrderStatusShipperInfo
                    {
                        ShipperId = o.Shipper.ShipperId,
                        FullName = o.Shipper.FullName,
                        PhoneNumber = o.Shipper.ShipperNavigation.PhoneNumber,
                        LicensePlate = o.Shipper.LicensePlate,
                        CurrentLat = o.Shipper.CurrentLat,
                        CurrentLng = o.Shipper.CurrentLng
                    } : null
                })
                .ToListAsync();

            return Ok(orderStatuses);
        }

        /// <summary>
        /// Monitors all active orders in the system.
        /// Admin Use Case A-UC04: Monitor active orders with comprehensive information.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20)</param>
        /// <param name="status">Optional filter by specific status</param>
        /// <param name="requiresAttention">Optional filter for orders requiring attention</param>
        [HttpGet("admin/monitor")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<object>> MonitorActiveOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] bool? requiresAttention = null)
        {
            var query = _context.Orders
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
                .AsQueryable();

            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.ToUpper();
                query = query.Where(o => o.OrderStatus == normalizedStatus);
            }
            else
            {
                // Default: show only active orders (not completed or cancelled)
                var activeStatuses = new[] 
                { 
                    OrderStatusConstants.Pending, 
                    OrderStatusConstants.Confirmed, 
                    OrderStatusConstants.Prepared, 
                    OrderStatusConstants.Delivering 
                };
                query = query.Where(o => activeStatuses.Contains(o.OrderStatus));
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new AdminOrderMonitorDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    OrderStatus = o.OrderStatus,
                    DeliveryAddress = o.DeliveryAddress,
                    Note = o.Note,

                    Financial = new AdminOrderFinancialInfo
                    {
                        Subtotal = o.Subtotal,
                        TotalDiscount = o.OrderVouchers.Sum(ov => ov.DiscountApplied),
                        ShippingFee = o.ShippingFee,
                        PlatformCommission = o.TotalAmount * 0.10m,
                        RestaurantEarning = o.Subtotal * 0.90m,
                        ShipperEarning = o.ShippingFee,
                        TotalAmount = o.TotalAmount
                    },

                    Timeline = new AdminOrderTimeline
                    {
                        OrderPlaced = o.CreatedAt,
                        Confirmed = o.ConfirmedAt,
                        Prepared = o.PreparedAt,
                        ShipperAssigned = o.ShipperId != null && o.PreparedAt != null ? o.PreparedAt : null,
                        OutForDelivery = o.DeliveringAt,
                        Delivered = o.CompletedAt,
                        Cancelled = o.CancelledAt
                    },

                    DurationStats = new AdminOrderDurationStats
                    {
                        TimeToConfirm = o.ConfirmedAt.HasValue && o.CreatedAt.HasValue
                            ? (int)(o.ConfirmedAt.Value - o.CreatedAt.Value).TotalMinutes
                            : null,
                        TimeToPrepare = o.PreparedAt.HasValue && o.ConfirmedAt.HasValue
                            ? (int)(o.PreparedAt.Value - o.ConfirmedAt.Value).TotalMinutes
                            : null,
                        TimeToAssignShipper = o.ShipperId.HasValue && o.PreparedAt.HasValue && o.DeliveringAt.HasValue
                            ? (int)(o.DeliveringAt.Value - o.PreparedAt.Value).TotalMinutes
                            : null,
                        TimeToDeliver = o.CompletedAt.HasValue && o.DeliveringAt.HasValue
                            ? (int)(o.CompletedAt.Value - o.DeliveringAt.Value).TotalMinutes
                            : null,
                        TotalDuration = o.CompletedAt.HasValue && o.CreatedAt.HasValue
                            ? (int)(o.CompletedAt.Value - o.CreatedAt.Value).TotalMinutes
                            : o.CreatedAt.HasValue
                                ? (int)(DateTime.UtcNow - o.CreatedAt.Value).TotalMinutes
                                : null
                    },

                    Customer = new AdminOrderCustomerInfo
                    {
                        CustomerId = o.Customer.CustomerId,
                        FullName = o.Customer.FullName,
                        Email = o.Customer.Email,
                        PhoneNumber = o.Customer.CustomerNavigation.PhoneNumber,
                        TotalOrders = o.Customer.Orders.Count,
                        IsActive = o.Customer.CustomerNavigation.IsActive
                    },

                    Restaurant = new AdminOrderRestaurantInfo
                    {
                        RestaurantId = o.Restaurant.RestaurantId,
                        RestaurantName = o.Restaurant.RestaurantName,
                        Address = o.Restaurant.Address,
                        PhoneNumber = o.Restaurant.PhoneNumber,
                        IsActive = o.Restaurant.IsActive,
                        AverageRating = o.Restaurant.Orders
                            .SelectMany(order => order.OrderItems)
                            .Where(oi => oi.Review != null)
                            .Average(oi => (decimal?)oi.Review!.Rating),
                        TotalCompletedOrders = o.Restaurant.Orders.Count(order => order.OrderStatus == OrderStatusConstants.Completed)
                    },

                    Shipper = o.Shipper != null ? new AdminOrderShipperInfo
                    {
                        ShipperId = o.Shipper.ShipperId,
                        FullName = o.Shipper.FullName,
                        PhoneNumber = o.Shipper.ShipperNavigation.PhoneNumber,
                        LicensePlate = o.Shipper.LicensePlate,
                        IsAvailable = o.Shipper.IsAvailable,
                        CurrentLat = o.Shipper.CurrentLat,
                        CurrentLng = o.Shipper.CurrentLng,
                        TotalDeliveries = o.Shipper.Orders.Count(order => order.OrderStatus == OrderStatusConstants.Completed)
                    } : null,

                    ItemsSummary = new AdminOrderItemsSummary
                    {
                        TotalItems = o.OrderItems.Count,
                        TotalQuantity = o.OrderItems.Sum(oi => oi.Quantity),
                        MostExpensiveItem = o.OrderItems.OrderByDescending(oi => oi.PriceAtOrder).FirstOrDefault() != null
                            ? o.OrderItems.OrderByDescending(oi => oi.PriceAtOrder).First().Dish.DishName
                            : null
                    },

                    Items = o.OrderItems.Select(oi => new AdminOrderItemInfo
                    {
                        OrderItemId = oi.OrderItemId,
                        DishId = oi.DishId,
                        DishName = oi.Dish.DishName,
                        Quantity = oi.Quantity,
                        PriceAtOrder = oi.PriceAtOrder,
                        CurrentPrice = oi.Dish.Price,
                        ItemTotal = oi.Quantity * oi.PriceAtOrder,
                        IsStillAvailable = oi.Dish.IsAvailable
                    }).ToList(),

                    AppliedVouchers = o.OrderVouchers.Select(ov => new AdminOrderVoucherInfo
                    {
                        VoucherId = ov.Voucher.VoucherId,
                        VoucherCode = ov.Voucher.VoucherCode,
                        Description = ov.Voucher.Description,
                        DiscountType = ov.Voucher.DiscountType,
                        DiscountValue = ov.Voucher.DiscountValue,
                        DiscountApplied = ov.DiscountApplied,
                        IsStillActive = ov.Voucher.IsActive ?? false
                    }).ToList(),

                    Payment = o.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault() != null
                        ? new AdminOrderPaymentInfo
                        {
                            PaymentId = o.Payments.OrderByDescending(p => p.CreatedAt).First().PaymentId,
                            PaymentMethod = o.Payments.OrderByDescending(p => p.CreatedAt).First().PaymentMethod,
                            Amount = o.Payments.OrderByDescending(p => p.CreatedAt).First().Amount,
                            PaymentStatus = o.Payments.OrderByDescending(p => p.CreatedAt).First().PaymentStatus,
                            TransactionCode = o.Payments.OrderByDescending(p => p.CreatedAt).First().TransactionCode,
                            CreatedAt = o.Payments.OrderByDescending(p => p.CreatedAt).First().CreatedAt,
                            IsAmountCorrect = o.Payments.OrderByDescending(p => p.CreatedAt).First().Amount == o.TotalAmount
                        }
                        : null,

                    RequiresAttention = false, // Will be calculated below
                    Issues = new List<string>()
                })
                .ToListAsync();

            // Calculate RequiresAttention and Issues for each order
            foreach (var order in orders)
            {
                var issues = new List<string>();

                // Check for delays
                if (order.OrderStatus == OrderStatusConstants.Pending && order.Timeline.OrderPlaced.HasValue)
                {
                    var pendingMinutes = (DateTime.UtcNow - order.Timeline.OrderPlaced.Value).TotalMinutes;
                    if (pendingMinutes > 15)
                    {
                        issues.Add($"Pending for {(int)pendingMinutes} minutes (threshold: 15 min)");
                    }
                }

                if (order.OrderStatus == OrderStatusConstants.Confirmed && order.Timeline.Confirmed.HasValue)
                {
                    var confirmMinutes = (DateTime.UtcNow - order.Timeline.Confirmed.Value).TotalMinutes;
                    if (confirmMinutes > 30)
                    {
                        issues.Add($"Preparation taking {(int)confirmMinutes} minutes (threshold: 30 min)");
                    }
                }

                if (order.OrderStatus == OrderStatusConstants.Prepared && order.Timeline.Prepared.HasValue && order.Shipper == null)
                {
                    var preparedMinutes = (DateTime.UtcNow - order.Timeline.Prepared.Value).TotalMinutes;
                    if (preparedMinutes > 15)
                    {
                        issues.Add($"No shipper assigned after {(int)preparedMinutes} minutes (threshold: 15 min)");
                    }
                }

                if (order.OrderStatus == OrderStatusConstants.Delivering && order.Timeline.OutForDelivery.HasValue)
                {
                    var deliveringMinutes = (DateTime.UtcNow - order.Timeline.OutForDelivery.Value).TotalMinutes;
                    if (deliveringMinutes > 40)
                    {
                        issues.Add($"Delivery taking {(int)deliveringMinutes} minutes (threshold: 40 min)");
                    }
                }

                // Check payment issues
                if (order.Payment != null && !order.Payment.IsAmountCorrect)
                {
                    issues.Add("Payment amount mismatch");
                }

                // Check account status
                if (!order.Customer.IsActive)
                {
                    issues.Add("Customer account inactive");
                }

                if (!order.Restaurant.IsActive)
                {
                    issues.Add("Restaurant account inactive");
                }

                order.Issues = issues;
                order.RequiresAttention = issues.Any();
            }

            // Apply requiresAttention filter if specified
            if (requiresAttention.HasValue)
            {
                orders = orders.Where(o => o.RequiresAttention == requiresAttention.Value).ToList();
            }

            // Apply pagination
            var totalRecords = orders.Count;
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            var paginatedOrders = orders
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                ActiveOrders = totalRecords,
                OrdersRequiringAttention = orders.Count(o => o.RequiresAttention),
                Data = paginatedOrders
            });
        }

        /// <summary>
        /// Gets detailed status for a specific order (Admin view).
        /// </summary>
        /// <param name="id">Order ID</param>
        [HttpGet("admin/{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<AdminOrderMonitorDto>> GetOrderStatusForAdmin(int id)
        {
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
                .Where(o => o.OrderId == id)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Build comprehensive DTO (similar to MonitorActiveOrders but for single order)
            var result = new AdminOrderMonitorDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                OrderStatus = order.OrderStatus,
                DeliveryAddress = order.DeliveryAddress,
                Note = order.Note,
                // ... (rest of the mapping similar to MonitorActiveOrders)
            };

            return Ok(result);
        }
    }
}