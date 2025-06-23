namespace OneTime.API.Models.DTOs;

// Request DTOs
public class DeviceRegistrationRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // ios, android
    public string? DeviceModel { get; set; }
    public string? AppVersion { get; set; }
}

public class NotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
    public string? Sound { get; set; }
    public int? Badge { get; set; }
}

public class NotificationSettingsRequest
{
    public bool Matches { get; set; } = true;
    public bool Messages { get; set; } = true;
    public bool Likes { get; set; } = true;
    public bool SuperLikes { get; set; } = true;
    public bool NewMatchMessages { get; set; } = true;
    public bool MarketingEmails { get; set; } = false;
    public bool PushNotifications { get; set; } = true;
    public bool EmailNotifications { get; set; } = true;
    public bool SMSNotifications { get; set; } = false;
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }
}

// Response DTOs
public class NotificationResponse
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? ImageUrl { get; set; }
}