namespace OneTime.API.Models.DTOs;

// Request DTOs
public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? Occupation { get; set; }
    public string? Education { get; set; }
    public int? Height { get; set; }
    public string? Drinking { get; set; }
    public string? Smoking { get; set; }
    public string? Children { get; set; }
    public string? Religion { get; set; }
    public string? PoliticalViews { get; set; }
}

public class UploadPhotoRequest
{
    public IFormFile Photo { get; set; } = null!;
    public int Order { get; set; }
    public bool IsMain { get; set; }
}

public class UpdatePhotoOrderRequest
{
    public int Order { get; set; }
}

public class UpdateLocationRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

public class PhoneVerificationRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyPhoneRequest
{
    public string Code { get; set; } = string.Empty;
}

public class PhotoVerificationRequest
{
    public IFormFile VerificationPhoto { get; set; } = null!;
}

public class UpdateInterestsRequest
{
    public List<string> InterestIds { get; set; } = new();
}

public class UpdateSettingsRequest
{
    public bool ShowMeOnDiscovery { get; set; } = true;
    public bool AllowLocationServices { get; set; } = true;
    public NotificationSettings Notifications { get; set; } = new();
    public PrivacySettings Privacy { get; set; } = new();
}

public class NotificationSettings
{
    public bool Matches { get; set; } = true;
    public bool Messages { get; set; } = true;
    public bool Likes { get; set; } = true;
    public bool SuperLikes { get; set; } = true;
    public bool MarketingEmails { get; set; } = false;
}

public class PrivacySettings
{
    public bool ShowLastActive { get; set; } = true;
    public bool ShowDistance { get; set; } = true;
    public bool ShowAge { get; set; } = true;
    public bool AllowReadReceipts { get; set; } = true;
}

public class SubscribePremiumRequest
{
    public string PlanId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class DeleteAccountRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Feedback { get; set; }
}

public class SubmitFeedbackRequest
{
    public string Type { get; set; } = string.Empty; // bug, feature, general
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5 stars
}

// Response DTOs
public class UserProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int Age { get; set; }
    public string? Bio { get; set; }
    public string? Occupation { get; set; }
    public string? Education { get; set; }
    public int? Height { get; set; }
    public string? Drinking { get; set; }
    public string? Smoking { get; set; }
    public string? Children { get; set; }
    public string? Religion { get; set; }
    public string? PoliticalViews { get; set; }
    public List<PhotoResponse> Photos { get; set; } = new();
    public List<string> Interests { get; set; } = new();
    public double? Distance { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? LastActive { get; set; }
    public bool IsPremium { get; set; }
    public bool IsOnline { get; set; }
}

public class PhotoResponse
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Order { get; set; }
    public bool IsMain { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InterestResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Icon { get; set; }
}

public class UserSettingsResponse
{
    public bool ShowMeOnDiscovery { get; set; }
    public bool AllowLocationServices { get; set; }
    public NotificationSettings Notifications { get; set; } = new();
    public PrivacySettings Privacy { get; set; } = new();
}

public class SubscriptionResponse
{
    public string Id { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UserActivityResponse
{
    public int TotalLogins { get; set; }
    public int ProfileViews { get; set; }
    public int LikesGiven { get; set; }
    public int LikesReceived { get; set; }
    public int SuperLikesGiven { get; set; }
    public int SuperLikesReceived { get; set; }
    public int Matches { get; set; }
    public int MessagesSent { get; set; }
    public int MessagesReceived { get; set; }
    public DateTime LastActive { get; set; }
    public List<DailyActivityResponse> DailyActivity { get; set; } = new();
}

public class DailyActivityResponse
{
    public DateTime Date { get; set; }
    public int Logins { get; set; }
    public int LikesGiven { get; set; }
    public int MessagesSent { get; set; }
    public TimeSpan TimeSpent { get; set; }
}

public class UserStatisticsResponse
{
    public int TotalMatches { get; set; }
    public int ActiveConversations { get; set; }
    public double AverageResponseTime { get; set; } // in minutes
    public double MatchRate { get; set; } // percentage
    public string MostActiveDay { get; set; } = string.Empty;
    public TimeSpan AverageSessionDuration { get; set; }
    public int TotalPhotosUploaded { get; set; }
    public int ProfileCompletionPercentage { get; set; }
}