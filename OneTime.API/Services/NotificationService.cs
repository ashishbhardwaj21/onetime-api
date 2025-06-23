using Microsoft.Azure.NotificationHubs;
using OneTime.API.Models.DTOs;
using OneTime.API.Models.Entities;
using System.Text.Json;

namespace OneTime.API.Services;

public interface INotificationService
{
    Task<ServiceResult<bool>> RegisterDeviceAsync(string userId, DeviceRegistrationRequest request);
    Task<ServiceResult<bool>> UnregisterDeviceAsync(string userId, string deviceToken);
    Task<ServiceResult<bool>> SendNotificationAsync(string userId, NotificationRequest request);
    Task SendMatchNotificationAsync(string userId, string matchedUserId);
    Task SendLikeNotificationAsync(string userId, string likerUserId);
    Task SendSuperLikeNotificationAsync(string userId, string superLikerUserId);
    Task SendMessageNotificationAsync(string userId, MessageResponse message);
    Task<ServiceResult<List<NotificationResponse>>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 20);
    Task<ServiceResult<bool>> MarkNotificationAsReadAsync(string userId, string notificationId);
    Task<ServiceResult<bool>> MarkAllNotificationsAsReadAsync(string userId);
    Task<ServiceResult<int>> GetUnreadNotificationCountAsync(string userId);
    Task<ServiceResult<bool>> UpdateNotificationSettingsAsync(string userId, NotificationSettingsRequest request);
}

public class NotificationService : INotificationService
{
    private readonly NotificationHubClient _notificationHubClient;
    private readonly IUserService _userService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUserService userService,
        IAnalyticsService analyticsService,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _userService = userService;
        _analyticsService = analyticsService;
        _configuration = configuration;
        _logger = logger;

        var connectionString = _configuration.GetConnectionString("NotificationHubs");
        var hubName = _configuration["Azure:NotificationHubs:HubName"];
        
        if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(hubName))
        {
            _notificationHubClient = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);
        }
        else
        {
            _logger.LogWarning("Notification Hub configuration is missing");
        }
    }

    public async Task<ServiceResult<bool>> RegisterDeviceAsync(string userId, DeviceRegistrationRequest request)
    {
        try
        {
            if (_notificationHubClient == null)
            {
                return ServiceResult<bool>.Failure("Notification service not configured");
            }

            var tags = new List<string> { $"user_{userId}" };
            
            // Add platform-specific tags
            if (request.Platform?.ToLower() == "ios")
            {
                tags.Add("ios");
            }
            else if (request.Platform?.ToLower() == "android")
            {
                tags.Add("android");
            }

            // Register with platform-specific method
            NotificationOutcome outcome;
            if (request.Platform?.ToLower() == "ios")
            {
                outcome = await _notificationHubClient.CreateAppleNativeRegistrationAsync(request.DeviceToken, tags);
            }
            else
            {
                outcome = await _notificationHubClient.CreateFcmNativeRegistrationAsync(request.DeviceToken, tags);
            }

            _logger.LogInformation("Device registered successfully for user {UserId}", userId);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device for user {UserId}", userId);
            return ServiceResult<bool>.Failure("Failed to register device");
        }
    }

    public async Task<ServiceResult<bool>> UnregisterDeviceAsync(string userId, string deviceToken)
    {
        try
        {
            if (_notificationHubClient == null)
            {
                return ServiceResult<bool>.Failure("Notification service not configured");
            }

            // Find and delete registrations for this device token
            var registrations = await _notificationHubClient.GetRegistrationsByChannelAsync(deviceToken, 100);
            
            foreach (var registration in registrations)
            {
                await _notificationHubClient.DeleteRegistrationAsync(registration);
            }

            _logger.LogInformation("Device unregistered successfully for user {UserId}", userId);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device for user {UserId}", userId);
            return ServiceResult<bool>.Failure("Failed to unregister device");
        }
    }

    public async Task<ServiceResult<bool>> SendNotificationAsync(string userId, NotificationRequest request)
    {
        try
        {
            if (_notificationHubClient == null)
            {
                return ServiceResult<bool>.Failure("Notification service not configured");
            }

            var tag = $"user_{userId}";
            
            // Create platform-specific notifications
            var iosPayload = JsonSerializer.Serialize(new
            {
                aps = new
                {
                    alert = new
                    {
                        title = request.Title,
                        body = request.Body
                    },
                    badge = request.Badge ?? 1,
                    sound = request.Sound ?? "default"
                },
                type = request.Type,
                data = request.Data
            });

            var androidPayload = JsonSerializer.Serialize(new
            {
                data = new
                {
                    title = request.Title,
                    body = request.Body,
                    type = request.Type,
                    data = JsonSerializer.Serialize(request.Data)
                }
            });

            // Send to iOS devices
            await _notificationHubClient.SendAppleNativeNotificationAsync(iosPayload, tag);
            
            // Send to Android devices
            await _notificationHubClient.SendFcmNativeNotificationAsync(androidPayload, tag);

            // Track analytics
            await _analyticsService.TrackEventAsync(userId, "notification_sent", new Dictionary<string, object>
            {
                {"type", request.Type},
                {"timestamp", DateTime.UtcNow}
            });

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            return ServiceResult<bool>.Failure("Failed to send notification");
        }
    }

    public async Task SendMatchNotificationAsync(string userId, string matchedUserId)
    {
        try
        {
            var matchedUser = await _userService.GetUserProfileAsync(matchedUserId);
            if (!matchedUser.Success || matchedUser.Data == null)
            {
                return;
            }

            var notification = new NotificationRequest
            {
                Title = "It's a Match! 🎉",
                Body = $"You and {matchedUser.Data.Name} liked each other!",
                Type = "match",
                Sound = "match_sound.mp3",
                Badge = 1,
                Data = new Dictionary<string, object>
                {
                    {"match_id", Guid.NewGuid().ToString()},
                    {"matched_user_id", matchedUserId},
                    {"matched_user_name", matchedUser.Data.Name}
                }
            };

            await SendNotificationAsync(userId, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending match notification to user {UserId}", userId);
        }
    }

    public async Task SendLikeNotificationAsync(string userId, string likerUserId)
    {
        try
        {
            var likerUser = await _userService.GetUserProfileAsync(likerUserId);
            if (!likerUser.Success || likerUser.Data == null)
            {
                return;
            }

            var notification = new NotificationRequest
            {
                Title = "Someone likes you! 💕",
                Body = $"{likerUser.Data.Name} liked your profile",
                Type = "like",
                Badge = 1,
                Data = new Dictionary<string, object>
                {
                    {"liker_user_id", likerUserId},
                    {"liker_user_name", likerUser.Data.Name}
                }
            };

            await SendNotificationAsync(userId, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending like notification to user {UserId}", userId);
        }
    }

    public async Task SendSuperLikeNotificationAsync(string userId, string superLikerUserId)
    {
        try
        {
            var superLikerUser = await _userService.GetUserProfileAsync(superLikerUserId);
            if (!superLikerUser.Success || superLikerUser.Data == null)
            {
                return;
            }

            var notification = new NotificationRequest
            {
                Title = "Someone Super Liked you! ⭐",
                Body = $"{superLikerUser.Data.Name} super liked your profile",
                Type = "super_like",
                Sound = "super_like_sound.mp3",
                Badge = 1,
                Data = new Dictionary<string, object>
                {
                    {"super_liker_user_id", superLikerUserId},
                    {"super_liker_user_name", superLikerUser.Data.Name}
                }
            };

            await SendNotificationAsync(userId, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending super like notification to user {UserId}", userId);
        }
    }

    public async Task SendMessageNotificationAsync(string userId, MessageResponse message)
    {
        try
        {
            var senderUser = await _userService.GetUserProfileAsync(message.SenderId);
            if (!senderUser.Success || senderUser.Data == null)
            {
                return;
            }

            var notification = new NotificationRequest
            {
                Title = senderUser.Data.Name,
                Body = GetMessagePreview(message),
                Type = "message",
                Badge = 1,
                Data = new Dictionary<string, object>
                {
                    {"conversation_id", message.ConversationId},
                    {"message_id", message.Id},
                    {"sender_id", message.SenderId},
                    {"sender_name", senderUser.Data.Name}
                }
            };

            await SendNotificationAsync(userId, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message notification to user {UserId}", userId);
        }
    }

    private static string GetMessagePreview(MessageResponse message)
    {
        return message.Type switch
        {
            "text" => message.Content ?? "Sent a message",
            "image" => "📷 Sent a photo",
            "video" => "🎥 Sent a video",
            "voice" => "🎤 Sent a voice message",
            "gif" => "📸 Sent a GIF",
            _ => "Sent a message"
        };
    }

    // Implement remaining interface methods with NotImplementedException for now
    public async Task<ServiceResult<List<NotificationResponse>>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 20)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> MarkNotificationAsReadAsync(string userId, string notificationId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> MarkAllNotificationsAsReadAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<int>> GetUnreadNotificationCountAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> UpdateNotificationSettingsAsync(string userId, NotificationSettingsRequest request)
    {
        throw new NotImplementedException();
    }
}