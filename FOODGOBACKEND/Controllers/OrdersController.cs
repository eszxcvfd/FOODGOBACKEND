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
    public class OrdersController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public OrdersController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new order for a customer.
        /// Customer Use Case C-UC05: Create order.
        /// </summary>
        /// <param name="dto">Order creation data</param>
        [HttpPost]
        [Authorize(Roles = "CUSTOMER")]
        public async Task<ActionResult<OrderDetailsDto>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            // Validate restaurant exists and is active
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.RestaurantId == dto.RestaurantId && r.IsActive);

            if (restaurant == null)
            {
                return BadRequest("Restaurant not found or not active.");
            }

            // Validate all dishes exist, are available, and belong to the restaurant
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

            // Calculate order totals
            decimal subtotal = 0;
            foreach (var item in dto.Items)
            {
                var dish = dishes.First(d => d.DishId == item.DishId);
                subtotal += dish.Price * item.Quantity;
            }

            // Calculate shipping fee (example: fixed 15,000 VND)
            decimal shippingFee = 15000m;

            // Apply voucher if provided
            decimal discountAmount = 0;
            OrderVoucher? orderVoucher = null;

            if (!string.IsNullOrEmpty(dto.VoucherCode))
            {
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.VoucherCode == dto.VoucherCode 
                                           && (v.IsActive ?? false)
                                           && v.ValidFrom <= DateTime.UtcNow
                                           && v.ValidTo >= DateTime.UtcNow
                                           && (v.CurrentUsage ?? 0) < (v.MaxUsage ?? int.MaxValue));

                if (voucher != null && subtotal >= voucher.MinOrderValue)
                {
                    // Calculate discount based on voucher type
                    if (voucher.DiscountType == "Percentage")
                    {
                        discountAmount = subtotal * (voucher.DiscountValue / 100);
                    }
                    else if (voucher.DiscountType == "FixedAmount")
                    {
                        discountAmount = voucher.DiscountValue;
                    }

                    // Create voucher application record (will be saved later)
                    orderVoucher = new OrderVoucher
                    {
                        VoucherId = voucher.VoucherId,
                        DiscountApplied = discountAmount
                    };

                    // Update voucher usage
                    voucher.CurrentUsage++;
                }
            }

            decimal totalAmount = subtotal + shippingFee - discountAmount;

            // Create order
            var order = new Order
            {
                CustomerId = customerId,
                RestaurantId = dto.RestaurantId,
                OrderCode = GenerateOrderCode(),
                DeliveryAddress = dto.DeliveryAddress,
                Note = dto.Note,
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                OrderStatus = OrderStatusConstants.Pending,
                CreatedAt = DateTime.UtcNow
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Save order to get OrderId
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items
                foreach (var item in dto.Items)
                {
                    var dish = dishes.First(d => d.DishId == item.DishId);
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        DishId = item.DishId,
                        Quantity = item.Quantity,
                        PriceAtOrder = dish.Price
                    };
                    _context.OrderItems.Add(orderItem);
                }

                // Save voucher application if exists
                if (orderVoucher != null)
                {
                    orderVoucher.OrderId = order.OrderId;
                    _context.OrderVouchers.Add(orderVoucher);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return order details
                var orderDetails = await GetOrderDetailsById(order.OrderId);
                return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, orderDetails);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Order creation error: {ex.Message}");
                return StatusCode(500, $"An error occurred while creating the order: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets order details by ID.
        /// </summary>
        /// <param name="id">Order ID</param>
        [HttpGet("{id}")]
        [Authorize(Roles = "CUSTOMER,RESTAURANT,SHIPPER")]
        public async Task<ActionResult<OrderDetailsDto>> GetOrderById(int id)
        {
            var orderDetails = await GetOrderDetailsById(id);

            if (orderDetails == null)
            {
                return NotFound("Order not found.");
            }

            // Verify access permission based on role
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            bool hasAccess = userRole switch
            {
                "CUSTOMER" => orderDetails.Customer.CustomerId == userId,
                "RESTAURANT" => await _context.Restaurants.AnyAsync(r => r.OwnerId == userId && r.RestaurantId == orderDetails.Restaurant.RestaurantId),
                "SHIPPER" => orderDetails.Shipper?.ShipperId == userId,
                _ => false
            };

            if (!hasAccess)
            {
                return Forbid("You do not have permission to view this order.");
            }

            return Ok(orderDetails);
        }

        /// <summary>
        /// Helper method to generate unique order code.
        /// </summary>
        private string GenerateOrderCode()
        {
            // Format: ORD + yyMMddHHmmss (12 digits) + random 4 digits = 19 characters total (fits in 20 char limit)
            return $"ORD{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        }

        /// <summary>
        /// Helper method to get order details by ID.
        /// </summary>
        private async Task<OrderDetailsDto?> GetOrderDetailsById(int orderId)
        {
            return await _context.Orders
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
                .Where(o => o.OrderId == orderId)
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
        }
    }
}