using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FOODGOBACKEND.Dtos.Restaurant;
using FOODGOBACKEND.Helpers;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "RESTAURANT")]
    public class RestaurantController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public RestaurantController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new dish with image upload.
        /// Restaurant Use Case R-UC07: Manage menu items.
        /// Restaurant ID is automatically determined from authenticated token.
        /// </summary>
        [HttpPost("foods")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<object>> CreateFood([FromForm] RequestFoodDto model)
        {
            if (model == null) return BadRequest("Invalid form data.");
            if (string.IsNullOrWhiteSpace(model.DishName))
                return BadRequest("Dish name is required.");
            if (model.Price < 0) return BadRequest("Price must be greater than or equal to 0.");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
                return Unauthorized("Invalid restaurant token.");

            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == ownerId);
            if (restaurant == null) return NotFound("Restaurant not found.");

            string? imageUrl = null;
            var imageFile = model.ImageUrl;
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("Invalid file type. Allowed: jpg, jpeg, png, gif, webp");
                if (imageFile.Length > 5 * 1024 * 1024)
                    return BadRequest("File size must not exceed 5MB.");

                // Save to wwwroot/dish
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var dishDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "dish");
                if (!Directory.Exists(dishDirectory)) Directory.CreateDirectory(dishDirectory);
                var filePath = Path.Combine(dishDirectory, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Store only filename in database
                imageUrl = fileName;
            }

            var dish = new Dish
            {
                RestaurantId = restaurant.RestaurantId,
                DishName = model.DishName,
                Description = model.Description,
                Price = model.Price,
                ImageUrl = imageUrl,
                IsAvailable = model.IsAvailable
            };

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();

            // Build full URL for response
            var fullImageUrl = !string.IsNullOrEmpty(dish.ImageUrl)
                ? $"{Request.Scheme}://{Request.Host}/dish/{dish.ImageUrl}"
                : null;

            var result = new ResponseFoodDto
            {
                DishId = dish.DishId,
                RestaurantId = dish.RestaurantId,
                DishName = dish.DishName,
                Description = dish.Description,
                Price = dish.Price,
                ImageUrl = fullImageUrl, // Return full URL
                IsAvailable = dish.IsAvailable
            };

            return Ok(new { Message = "Dish created successfully.", Data = result });
        }

        /// <summary>
        /// Updates an existing dish with optional image upload.
        /// Restaurant Use Case R-UC07: Manage menu items.
        /// If no image is provided, the existing image will be retained.
        /// </summary>
        [HttpPut("foods/{dishId}")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<object>> UpdateFood(int dishId, [FromForm] UpdateFoodDto model)
        {
            // === ENHANCED DEBUG ===
            Console.WriteLine($"=== UPDATE FOOD REQUEST ===");
            Console.WriteLine($"DishId from route: {dishId}");
            Console.WriteLine($"Content-Type: {Request.ContentType}");
            Console.WriteLine($"Has Form: {Request.HasFormContentType}");
            
            if (Request.HasFormContentType && Request.Form != null)
            {
                Console.WriteLine($"Form Keys Count: {Request.Form.Keys.Count}");
                foreach (var key in Request.Form.Keys)
                {
                    Console.WriteLine($"  Form[{key}] = {Request.Form[key]}");
                }
                
                Console.WriteLine($"Form Files Count: {Request.Form.Files.Count}");
                foreach (var file in Request.Form.Files)
                {
                    Console.WriteLine($"  File: {file.Name}, FileName: {file.FileName}, Length: {file.Length}");
                }
            }
            
            Console.WriteLine($"\nModel State Valid: {ModelState.IsValid}");
            Console.WriteLine($"Model is null: {model == null}");
            
            if (model != null)
            {
                Console.WriteLine($"Model.DishName: '{model.DishName}'");
                Console.WriteLine($"Model.Price: {model.Price}");
                Console.WriteLine($"Model.IsAvailable: {model.IsAvailable}");
                Console.WriteLine($"Model.Description: '{model.Description}'");
                Console.WriteLine($"Model.ImageUrl: {model.ImageUrl?.FileName ?? "null"}");
            }
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("\n=== MODEL STATE ERRORS ===");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}");
                    foreach (var err in error.Value.Errors)
                    {
                        Console.WriteLine($"  - {err.ErrorMessage}");
                        if (err.Exception != null)
                        {
                            Console.WriteLine($"    Exception: {err.Exception.Message}");
                        }
                    }
                }
            }
            Console.WriteLine("=== END DEBUG ===\n");
            // === END DEBUG ===

            if (model == null) return BadRequest("Invalid form data.");
            if (string.IsNullOrWhiteSpace(model.DishName))
                return BadRequest("Dish name is required.");
            if (model.Price < 0) return BadRequest("Price must be greater than or equal to 0.");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
                return Unauthorized("Invalid restaurant token.");

            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == ownerId);
            if (restaurant == null) return NotFound("Restaurant not found.");

            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.DishId == dishId && d.RestaurantId == restaurant.RestaurantId);
            if (dish == null) return NotFound("Dish not found or does not belong to your restaurant.");

            // Only process image update if a new file is provided
            var imageFile = model.ImageUrl;
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("Invalid file type. Allowed: jpg, jpeg, png, gif, webp");
                if (imageFile.Length > 5 * 1024 * 1024)
                    return BadRequest("File size must not exceed 5MB.");

                // Delete old image if exists
                if (!string.IsNullOrEmpty(dish.ImageUrl))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "dish", dish.ImageUrl);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        try { System.IO.File.Delete(oldImagePath); }
                        catch (Exception) { /* ignore deletion errors */ }
                    }
                }

                // Save new image to wwwroot/dish
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var dishDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "dish");
                if (!Directory.Exists(dishDirectory)) Directory.CreateDirectory(dishDirectory);
                var filePath = Path.Combine(dishDirectory, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                dish.ImageUrl = fileName;
            }

            // Update other fields
            dish.DishName = model.DishName;
            dish.Description = model.Description;
            dish.Price = model.Price;
            dish.IsAvailable = model.IsAvailable;

            await _context.SaveChangesAsync();

            // Build full URL for response
            var fullImageUrl = !string.IsNullOrEmpty(dish.ImageUrl)
                ? $"{Request.Scheme}://{Request.Host}/dish/{dish.ImageUrl}"
                : null;

            var result = new ResponseFoodDto
            {
                DishId = dish.DishId,
                RestaurantId = dish.RestaurantId,
                DishName = dish.DishName,
                Description = dish.Description,
                Price = dish.Price,
                ImageUrl = fullImageUrl, // Return full URL
                IsAvailable = dish.IsAvailable
            };

            return Ok(new { Message = "Dish updated successfully.", Data = result });
        }

        /// <summary>
        /// Deletes a dish/food.
        /// </summary>
        /// <param name="dishId">The ID of the dish to delete.</param>
        [HttpDelete("foods/{dishId}")]
        public async Task<ActionResult<object>> DeleteFood(int dishId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
                return Unauthorized("Invalid restaurant token.");

            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == ownerId);
            if (restaurant == null) return NotFound("Restaurant not found.");

            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.DishId == dishId && d.RestaurantId == restaurant.RestaurantId);
            if (dish == null) return NotFound("Dish not found or does not belong to your restaurant.");

            var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.DishId == dishId);
            if (hasOrders) return BadRequest("Cannot delete dish that has been ordered. Consider marking it as unavailable instead.");

            // Delete image from wwwroot/dish
            if (!string.IsNullOrEmpty(dish.ImageUrl))
            {
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "dish", dish.ImageUrl);
                if (System.IO.File.Exists(oldImagePath))
                {
                    try { System.IO.File.Delete(oldImagePath); }
                    catch (Exception) { /* ignore deletion errors */ }
                }
            }

            _context.Dishes.Remove(dish);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Dish deleted successfully.", DishId = dishId });
        }

        /// <summary>
        /// Gets all dishes/foods for the authenticated restaurant.
        /// Restaurant Use Case R-UC06: View menu items.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of items per page (default: 10).</param>
        /// <param name="isAvailable">Filter by availability (optional).</param>
        /// <returns>Paginated list of dishes for the restaurant.</returns>
        [HttpGet("foods")]
        public async Task<ActionResult<object>> GetFood(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isAvailable = null)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
                return Unauthorized("Invalid restaurant token.");

            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == ownerId);
            if (restaurant == null) return NotFound("Restaurant not found.");

            var query = _context.Dishes.Where(d => d.RestaurantId == restaurant.RestaurantId).AsQueryable();

            if (isAvailable.HasValue)
                query = query.Where(d => d.IsAvailable == isAvailable.Value);

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var dishes = await query
                .OrderBy(d => d.DishName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = dishes.Select(d => new ResponseFoodDto
            {
                DishId = d.DishId,
                RestaurantId = d.RestaurantId,
                DishName = d.DishName,
                Description = d.Description,
                Price = d.Price,
                ImageUrl = !string.IsNullOrEmpty(d.ImageUrl)
                    ? $"{Request.Scheme}://{Request.Host}/dish/{d.ImageUrl}"
                    : null,
                IsAvailable = d.IsAvailable
            }).ToList();

            return Ok(new
            {
                Message = $"Found {totalRecords} dishes.",
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                RestaurantId = restaurant.RestaurantId,
                RestaurantName = restaurant.RestaurantName,
                Data = result
            });
        }

        /// <summary>
        /// Gets a specific dish/food by ID.
        /// </summary>
        /// <param name="dishId">The ID of the dish.</param>
        [HttpGet("foods/{dishId}")]
        public async Task<ActionResult<ResponseFoodDto>> GetFoodById(int dishId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
                return Unauthorized("Invalid restaurant token.");

            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == ownerId);
            if (restaurant == null) return NotFound("Restaurant not found.");

            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.DishId == dishId && d.RestaurantId == restaurant.RestaurantId);
            if (dish == null) return NotFound("Dish not found or does not belong to your restaurant.");

            var result = new ResponseFoodDto
            {
                DishId = dish.DishId,
                RestaurantId = dish.RestaurantId,
                DishName = dish.DishName,
                Description = dish.Description,
                Price = dish.Price,
                ImageUrl = !string.IsNullOrEmpty(dish.ImageUrl)
                    ? $"{Request.Scheme}://{Request.Host}/dish/{dish.ImageUrl}"
                    : null,
                IsAvailable = dish.IsAvailable
            };

            return Ok(result);
        }

        /// <summary>
        /// Gets all orders for the authenticated restaurant.
        /// Restaurant Use Case R-UC01: View all received orders.
        /// OPTIMIZED VERSION: Batch geocoding + caching
        /// </summary>
        [HttpGet("orders")]
        public async Task<ActionResult<object>> GetOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
            {
                return Unauthorized("Invalid restaurant token.");
            }

            // Get restaurant information
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerId == ownerId);

            if (restaurant == null)
            {
                return NotFound("Restaurant not found.");
            }

            // Query orders for this restaurant
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .Where(o => o.RestaurantId == restaurant.RestaurantId)
                .AsQueryable();

            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.ToUpper();
                query = query.Where(o => o.OrderStatus == normalizedStatus);
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // Get orders with pagination
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTO
            var result = new List<ItemOrderDto>();

            foreach (var order in orders)
            {
                // ===== TEMPORARILY DISABLED: Distance calculation =====
                // TODO: Re-enable when geocoding performance is optimized
                // var distance = await GeoLocationHelper.CalculateDistanceBetweenAddressesSimple(
                //     restaurant.Address,
                //     order.DeliveryAddress
                // );
                // double distanceKm = distance ?? 0;
                
                double distanceKm = 0; // Temporarily set to 0
                // ===== END TEMPORARY CHANGE =====

                // Count total items in order
                var itemCount = order.OrderItems.Sum(oi => oi.Quantity);

                // Map status to Vietnamese display text
                var (statusDisplay, nextAction) = GetStatusDisplayAndNextAction(order.OrderStatus);

                result.Add(new ItemOrderDto
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode,
                    CustomerName = order.Customer.FullName,
                    ItemCount = itemCount,
                    Distance = distanceKm, // Always 0 for now
                    TotalPrice = order.TotalAmount,
                    Status = order.OrderStatus,
                    StatusDisplay = statusDisplay,
                    NextAction = nextAction
                });
            }

            return Ok(new
            {
                Message = $"Found {totalRecords} orders.",
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                RestaurantId = restaurant.RestaurantId,
                RestaurantName = restaurant.RestaurantName,
                Data = result
            });
        }

        /// <summary>
        /// Helper method to get status display text and next action based on order status.
        /// </summary>
        /// <param name="status">Current order status.</param>
        /// <returns>Tuple of (statusDisplay, nextAction).</returns>
        private (string statusDisplay, string nextAction) GetStatusDisplayAndNextAction(string status)
        {
            return status.ToUpper() switch
            {
                "PENDING" => ("Chờ xác nhận", "ConfirmOrder"),
                "CONFIRMED" => ("Đã xác nhận", "MarkAsPreparing"),
                "PREPARING" => ("Đang chuẩn bị", "MarkAsReady"),
                "READY" => ("Sẵn sàng giao", ""),
                "DELIVERING" => ("Đang giao hàng", ""),
                "COMPLETED" => ("Đã hoàn thành", ""),
                "CANCELLED" => ("Đã hủy", ""),
                _ => ("Không xác định", "")
            };
        }

        /// <summary>
        /// Confirms a pending order.
        /// Restaurant Use Case: Confirm order (PENDING → CONFIRMED).
        /// </summary>
        /// <param name="orderId">The ID of the order to confirm.</param>
        [HttpPost("orders/{orderId}/confirm")]
        public async Task<ActionResult<object>> ConfirmOrder(int orderId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
            {
                return Unauthorized("Invalid restaurant token.");
            }

            // Get restaurant
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerId == ownerId);

            if (restaurant == null)
            {
                return NotFound("Restaurant not found.");
            }

            // Get order
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.RestaurantId == restaurant.RestaurantId);

            if (order == null)
            {
                return NotFound("Order not found or does not belong to your restaurant.");
            }

            // Check if order is in PENDING status
            if (order.OrderStatus != "PENDING")
            {
                return BadRequest($"Order must be in PENDING status to confirm. Current status: {order.OrderStatus}");
            }

            // Update order status
            order.OrderStatus = "CONFIRMED";
            order.ConfirmedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Order confirmed successfully.",
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                Status = order.OrderStatus,
                ConfirmedAt = order.ConfirmedAt
            });
        }

        /// <summary>
        /// Marks an order as preparing.
        /// Restaurant Use Case R-UC02: Start preparing order (CONFIRMED → PREPARING).
        /// </summary>
        /// <param name="orderId">The ID of the order to mark as preparing.</param>
        [HttpPost("orders/{orderId}/prepare")]
        public async Task<ActionResult<object>> MarkOrderAsPreparing(int orderId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
            {
                return Unauthorized("Invalid restaurant token.");
            }

            // Get restaurant
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerId == ownerId);

            if (restaurant == null)
            {
                return NotFound("Restaurant not found.");
            }

            // Get order
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.RestaurantId == restaurant.RestaurantId);

            if (order == null)
            {
                return NotFound("Order not found or does not belong to your restaurant.");
            }

            // Check if order is in CONFIRMED status
            if (order.OrderStatus != "CONFIRMED")
            {
                return BadRequest($"Order must be in CONFIRMED status to mark as preparing. Current status: {order.OrderStatus}");
            }

            // Update order status
            order.OrderStatus = "PREPARING";
            order.PreparedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Order marked as preparing successfully.",
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                Status = order.OrderStatus,
                PreparedAt = order.PreparedAt
            });
        }

        /// <summary>
        /// Marks an order as ready for pickup.
        /// Restaurant Use Case R-UC03: Mark order as ready.
        /// </summary>
        /// <param name="orderId">The ID of the order to mark as ready.</param>
        [HttpPost("orders/{orderId}/ready")]
        public async Task<ActionResult<object>> MarkOrderAsReady(int orderId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
            {
                return Unauthorized("Invalid restaurant token.");
            }

            // Get restaurant
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerId == ownerId);

            if (restaurant == null)
            {
                return NotFound("Restaurant not found.");
            }

            // Get order
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.RestaurantId == restaurant.RestaurantId);

            if (order == null)
            {
                return NotFound("Order not found or does not belong to your restaurant.");
            }

            // Check if order is in PREPARING status
            if (order.OrderStatus != "PREPARING")
            {
                return BadRequest($"Order must be in PREPARING status to mark as ready. Current status: {order.OrderStatus}");
            }

            // Update order status (Note: There's no ReadyAt field in the Order model, 
            // but we can use PreparedAt as the time when order was marked ready)
            order.OrderStatus = "DELIVERING";
            order.DeliveringAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Order marked as ready and handed to shipper successfully.",
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                Status = order.OrderStatus,
                DeliveringAt = order.DeliveringAt
            });
        }

        /// <summary>
        /// Cancels an order.
        /// Restaurant Use Case R-UC04: Cancel order.
        /// </summary>
        /// <param name="orderId">The ID of the order to cancel.</param>
        /// <param name="reason">Reason for cancellation.</param>
        [HttpPost("orders/{orderId}/cancel")]
        public async Task<ActionResult<object>> CancelOrder(int orderId, [FromBody] string? reason = null)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
            {
                return Unauthorized("Invalid restaurant token.");
            }

            // Get restaurant
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerId == ownerId);

            if (restaurant == null)
            {
                return NotFound("Restaurant not found.");
            }

            // Get order
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.RestaurantId == restaurant.RestaurantId);

            if (order == null)
            {
                return NotFound("Order not found or does not belong to your restaurant.");
            }

            // Check if order can be cancelled
            if (order.OrderStatus == "COMPLETED" || order.OrderStatus == "CANCELLED")
            {
                return BadRequest($"Cannot cancel order with status: {order.OrderStatus}");
            }

            // Update order status
            order.OrderStatus = "CANCELLED";
            order.CancelledAt = DateTime.UtcNow;
            
            // If reason provided, you might want to save it in a Note field or create a cancellation reason table
            if (!string.IsNullOrEmpty(reason))
            {
                order.Note = $"Cancelled by restaurant: {reason}";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Order cancelled successfully.",
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                Status = order.OrderStatus,
                CancelledAt = order.CancelledAt,
                Reason = reason
            });
        }

        /// <summary>
        /// Gets detailed information for a specific order.
        /// Restaurant Use Case R-UC05: View order details.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        [HttpGet("orders/{orderId}")]
        public async Task<ActionResult<object>> GetOrderDetail(int orderId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var ownerId))
            {
                return Unauthorized("Invalid restaurant token.");
            }

            // Get restaurant
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerId == ownerId);

            if (restaurant == null)
            {
                return NotFound("Restaurant not found.");
            }

            // Get order with all related data
            var order = await _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c.CustomerNavigation)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .Include(o => o.Shipper)
                    .ThenInclude(s => s!.ShipperNavigation)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.RestaurantId == restaurant.RestaurantId);

            if (order == null)
            {
                return NotFound("Order not found or does not belong to your restaurant.");
            }

            // Map order items
            var items = order.OrderItems.Select(oi => new
            {
                DishId = oi.DishId,
                DishName = oi.Dish.DishName,
                Quantity = oi.Quantity,
                Price = oi.PriceAtOrder,
                Total = oi.Quantity * oi.PriceAtOrder
            }).ToList();

            // Shipper info (if assigned)
            object? shipperInfo = null;
            if (order.Shipper != null)
            {
                shipperInfo = new
                {
                    ShipperId = order.Shipper.ShipperId,
                    ShipperName = order.Shipper.FullName,
                    ShipperPhone = order.Shipper.ShipperNavigation.PhoneNumber,
                    LicensePlate = order.Shipper.LicensePlate
                };
            }

            var (statusDisplay, nextAction) = GetStatusDisplayAndNextAction(order.OrderStatus);

            return Ok(new
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                Status = order.OrderStatus,
                StatusDisplay = statusDisplay,
                NextAction = nextAction,
                Customer = new
                {
                    CustomerId = order.Customer.CustomerId,
                    CustomerName = order.Customer.FullName,
                    CustomerPhone = order.Customer.CustomerNavigation.PhoneNumber,
                    DeliveryAddress = order.DeliveryAddress
                },
                Shipper = shipperInfo,
                Items = items,
                Subtotal = order.Subtotal,
                ShippingFee = order.ShippingFee,
                TotalAmount = order.TotalAmount,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                ConfirmedAt = order.ConfirmedAt,
                PreparedAt = order.PreparedAt,
                DeliveringAt = order.DeliveringAt,
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancelledAt
            });
        }
    }
}