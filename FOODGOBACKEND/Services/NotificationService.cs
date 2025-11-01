using FOODGOBACKEND.Models;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using DbNotification = FOODGOBACKEND.Models.Notification; // Thêm alias

namespace FOODGOBACKEND.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, Dictionary<string, string>? data = null);
        Task SendNotificationToMultipleUsersAsync(List<int> userIds, string title, string message, Dictionary<string, string>? data = null);
        Task SaveNotificationToDatabase(int userId, string title, string message);
    }

    public class NotificationService : INotificationService
    {
        private readonly FoodGoContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly FirebaseMessaging? _firebaseMessaging;

        public NotificationService(
            FoodGoContext context, 
            ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;

            // Initialize Firebase Admin SDK
            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile("firebase-adminsdk.json")
                    });
                }
                _firebaseMessaging = FirebaseMessaging.DefaultInstance;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize Firebase: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends push notification to a specific user and saves to database
        /// </summary>
        public async Task SendNotificationAsync(int userId, string title, string message, Dictionary<string, string>? data = null)
        {
            try
            {
                // 1. Save notification to database
                await SaveNotificationToDatabase(userId, title, message);

                // 2. Get all active device tokens for the user
                var deviceTokens = await _context.UserDevices
                    .Where(d => d.UserId == userId && d.IsActive)
                    .Select(d => d.DeviceToken)
                    .ToListAsync();

                if (!deviceTokens.Any())
                {
                    _logger.LogWarning($"No active devices found for user {userId}");
                    return;
                }

                // 3. Send FCM notification to all user's devices
                await SendFCMNotification(deviceTokens, title, message, data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending notification to user {userId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends notification to multiple users
        /// </summary>
        public async Task SendNotificationToMultipleUsersAsync(List<int> userIds, string title, string message, Dictionary<string, string>? data = null)
        {
            try
            {
                // 1. Save notifications to database for all users
                var notifications = userIds.Select(userId => new DbNotification // Sử dụng alias
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _context.Notifications.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();

                // 2. Get all active device tokens for all users
                var deviceTokens = await _context.UserDevices
                    .Where(d => userIds.Contains(d.UserId) && d.IsActive)
                    .Select(d => d.DeviceToken)
                    .ToListAsync();

                if (!deviceTokens.Any())
                {
                    _logger.LogWarning($"No active devices found for users: {string.Join(", ", userIds)}");
                    return;
                }

                // 3. Send FCM notification
                await SendFCMNotification(deviceTokens, title, message, data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending notifications to multiple users: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves notification to database only (no push notification)
        /// </summary>
        public async Task SaveNotificationToDatabase(int userId, string title, string message)
        {
            var notification = new DbNotification // Sử dụng alias
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Sends FCM push notification to device tokens
        /// </summary>
        private async Task SendFCMNotification(List<string> deviceTokens, string title, string message, Dictionary<string, string>? data = null)
        {
            if (_firebaseMessaging == null)
            {
                _logger.LogWarning("Firebase Messaging is not initialized. Skipping FCM notification.");
                return;
            }

            try
            {
                var notification = new FirebaseAdmin.Messaging.Notification // Full namespace
                {
                    Title = title,
                    Body = message
                };

                // Send to multiple devices
                var multicastMessage = new MulticastMessage
                {
                    Tokens = deviceTokens,
                    Notification = notification,
                    Data = data ?? new Dictionary<string, string>()
                };

                var response = await _firebaseMessaging.SendEachForMulticastAsync(multicastMessage); // Sử dụng method mới

                _logger.LogInformation($"Successfully sent {response.SuccessCount} notifications out of {deviceTokens.Count}");

                // Handle failed tokens (optional: remove invalid tokens)
                if (response.FailureCount > 0)
                {
                    var failedTokens = new List<string>();
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        if (!response.Responses[i].IsSuccess)
                        {
                            failedTokens.Add(deviceTokens[i]);
                            _logger.LogWarning($"Failed to send to token {deviceTokens[i]}: {response.Responses[i].Exception?.Message}");
                        }
                    }

                    // Remove invalid tokens from database
                    await RemoveInvalidTokens(failedTokens);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending FCM notification: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Removes invalid or expired device tokens
        /// </summary>
        private async Task RemoveInvalidTokens(List<string> invalidTokens)
        {
            try
            {
                var devicesToUpdate = await _context.UserDevices
                    .Where(d => invalidTokens.Contains(d.DeviceToken))
                    .ToListAsync();

                foreach (var device in devicesToUpdate)
                {
                    device.IsActive = false;
                    device.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deactivated {devicesToUpdate.Count} invalid device tokens");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing invalid tokens: {ex.Message}");
            }
        }
    }
}
