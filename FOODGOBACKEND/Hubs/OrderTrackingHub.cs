using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FOODGOBACKEND.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time order tracking.
    /// Handles connections from customers and admins to receive order status updates.
    /// </summary>
    [Authorize]
    public class OrderTrackingHub : Hub
    {
        private readonly ILogger<OrderTrackingHub> _logger;

        public OrderTrackingHub(ILogger<OrderTrackingHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            
            _logger.LogInformation($"User {userId} ({userRole}) connected to OrderTrackingHub. ConnectionId: {Context.ConnectionId}");
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (exception != null)
            {
                _logger.LogError(exception, $"User {userId} disconnected with error. ConnectionId: {Context.ConnectionId}");
            }
            else
            {
                _logger.LogInformation($"User {userId} disconnected. ConnectionId: {Context.ConnectionId}");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Allows a customer to join a specific order group to receive updates for that order.
        /// Customer Use Case C-UC07: Track order status in real-time.
        /// </summary>
        /// <param name="orderId">The order ID to track</param>
        [Authorize(Roles = "CUSTOMER")]
        public async Task JoinOrderGroup(int orderId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var groupName = $"Order_{orderId}";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation($"Customer {userId} joined order group: {groupName}. ConnectionId: {Context.ConnectionId}");
            
            // Notify the client that they successfully joined
            await Clients.Caller.SendAsync("JoinedOrderGroup", new
            {
                OrderId = orderId,
                Message = $"Successfully joined tracking for order {orderId}",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Allows a customer to leave a specific order group.
        /// </summary>
        /// <param name="orderId">The order ID to stop tracking</param>
        [Authorize(Roles = "CUSTOMER")]
        public async Task LeaveOrderGroup(int orderId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var groupName = $"Order_{orderId}";
            
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation($"Customer {userId} left order group: {groupName}. ConnectionId: {Context.ConnectionId}");
            
            await Clients.Caller.SendAsync("LeftOrderGroup", new
            {
                OrderId = orderId,
                Message = $"Stopped tracking order {orderId}",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Allows an admin to join the monitoring group to receive updates for all orders.
        /// Admin Use Case A-UC04: Monitor all active orders in real-time.
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        public async Task JoinAdminMonitoringGroup()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var groupName = "AdminMonitoring";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation($"Admin {userId} joined admin monitoring group. ConnectionId: {Context.ConnectionId}");
            
            await Clients.Caller.SendAsync("JoinedAdminMonitoring", new
            {
                Message = "Successfully joined admin monitoring",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Allows an admin to leave the monitoring group.
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        public async Task LeaveAdminMonitoringGroup()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var groupName = "AdminMonitoring";
            
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation($"Admin {userId} left admin monitoring group. ConnectionId: {Context.ConnectionId}");
            
            await Clients.Caller.SendAsync("LeftAdminMonitoring", new
            {
                Message = "Left admin monitoring",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Allows client to request current order status.
        /// </summary>
        /// <param name="orderId">The order ID to check</param>
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        public async Task RequestOrderStatus(int orderId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            _logger.LogInformation($"User {userId} requested status for order {orderId}");
            
            // This would typically fetch from database, but for now just acknowledge
            await Clients.Caller.SendAsync("OrderStatusRequested", new
            {
                OrderId = orderId,
                Message = "Status request received. Updates will be sent when available.",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Ping-pong mechanism to keep connection alive and check latency.
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", new
            {
                Timestamp = DateTime.UtcNow
            });
        }
    }
}