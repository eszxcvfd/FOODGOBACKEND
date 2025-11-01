using FOODGOBACKEND.Dtos.Order;
using FOODGOBACKEND.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FOODGOBACKEND.Services
{
    /// <summary>
    /// Service to push order status updates to connected clients via SignalR.
    /// </summary>
    public interface IOrderTrackingService
    {
        Task NotifyOrderStatusChanged(int orderId, string newStatus, OrderStatusDto orderStatus);
        Task NotifyOrderStatusChangedForAdmin(AdminOrderMonitorDto orderMonitor);
        Task NotifyShipperLocationUpdated(int orderId, decimal lat, decimal lng);
        Task NotifyOrderAssignedToShipper(int orderId, int shipperId, string shipperName);
    }

    public class OrderTrackingService : IOrderTrackingService
    {
        private readonly IHubContext<OrderTrackingHub> _hubContext;
        private readonly ILogger<OrderTrackingService> _logger;

        public OrderTrackingService(
            IHubContext<OrderTrackingHub> hubContext,
            ILogger<OrderTrackingService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Notifies customers tracking a specific order about status changes.
        /// </summary>
        public async Task NotifyOrderStatusChanged(int orderId, string newStatus, OrderStatusDto orderStatus)
        {
            var groupName = $"Order_{orderId}";
            
            _logger.LogInformation($"Notifying group {groupName} about status change to {newStatus}");
            
            await _hubContext.Clients.Group(groupName).SendAsync("OrderStatusUpdated", new
            {
                OrderId = orderId,
                NewStatus = newStatus,
                OrderStatus = orderStatus,
                Timestamp = DateTime.UtcNow,
                Message = GetStatusChangeMessage(newStatus)
            });
        }

        /// <summary>
        /// Notifies admins monitoring all orders about status changes.
        /// </summary>
        public async Task NotifyOrderStatusChangedForAdmin(AdminOrderMonitorDto orderMonitor)
        {
            var groupName = "AdminMonitoring";
            
            _logger.LogInformation($"Notifying admin group about order {orderMonitor.OrderId} status: {orderMonitor.OrderStatus}");
            
            await _hubContext.Clients.Group(groupName).SendAsync("AdminOrderUpdated", new
            {
                OrderId = orderMonitor.OrderId,
                OrderCode = orderMonitor.OrderCode,
                OrderStatus = orderMonitor.OrderStatus,
                OrderMonitor = orderMonitor,
                Timestamp = DateTime.UtcNow,
                RequiresAttention = orderMonitor.RequiresAttention,
                Issues = orderMonitor.Issues
            });
        }

        /// <summary>
        /// Notifies customers about shipper location updates (for real-time tracking on map).
        /// </summary>
        public async Task NotifyShipperLocationUpdated(int orderId, decimal lat, decimal lng)
        {
            var groupName = $"Order_{orderId}";
            
            await _hubContext.Clients.Group(groupName).SendAsync("ShipperLocationUpdated", new
            {
                OrderId = orderId,
                Location = new
                {
                    Latitude = lat,
                    Longitude = lng
                },
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Notifies when a shipper is assigned to an order.
        /// </summary>
        public async Task NotifyOrderAssignedToShipper(int orderId, int shipperId, string shipperName)
        {
            var groupName = $"Order_{orderId}";
            
            _logger.LogInformation($"Notifying group {groupName} about shipper assignment: {shipperName}");
            
            await _hubContext.Clients.Group(groupName).SendAsync("ShipperAssigned", new
            {
                OrderId = orderId,
                ShipperId = shipperId,
                ShipperName = shipperName,
                Timestamp = DateTime.UtcNow,
                Message = $"Your order has been assigned to shipper: {shipperName}"
            });

            // Also notify admins
            await _hubContext.Clients.Group("AdminMonitoring").SendAsync("OrderShipperAssigned", new
            {
                OrderId = orderId,
                ShipperId = shipperId,
                ShipperName = shipperName,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Helper method to generate user-friendly status change messages.
        /// </summary>
        private string GetStatusChangeMessage(string status)
        {
            return status switch
            {
                OrderStatusConstants.Pending => "Your order has been placed and is awaiting confirmation.",
                OrderStatusConstants.Confirmed => "Your order has been confirmed by the restaurant!",
                OrderStatusConstants.Prepared => "Your order is ready and waiting for a shipper.",
                OrderStatusConstants.Delivering => "Your order is on the way!",
                OrderStatusConstants.Completed => "Your order has been delivered. Enjoy your meal!",
                OrderStatusConstants.Cancelled => "Your order has been cancelled.",
                _ => $"Order status updated to {status}"
            };
        }
    }
}