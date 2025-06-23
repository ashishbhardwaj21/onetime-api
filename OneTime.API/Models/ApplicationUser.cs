using Microsoft.AspNetCore.Identity;
using OneTime.API.Models.Entities;

namespace OneTime.API.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; } = false;
    public bool IsVerified { get; set; } = false;
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public string? TwoFactorSecret { get; set; }
    public bool IsTwoFactorEnabled { get; set; } = false;
    public string? ProfilePhotoUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? InterestedIn { get; set; }
    public string? Occupation { get; set; }
    public string? Education { get; set; }
    public string? Bio { get; set; }
    public decimal? Height { get; set; }
    public string? Ethnicity { get; set; }
    public string? Religion { get; set; }
    public string? Drinking { get; set; }
    public string? Smoking { get; set; }
    public string? Children { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public DateTime? LastLocationUpdate { get; set; }
    public bool ShowDistance { get; set; } = true;
    public int? MaxDistance { get; set; } = 50;
    public int? MinAge { get; set; } = 18;
    public int? MaxAge { get; set; } = 99;
    public bool ShowMeOnDiscovery { get; set; } = true;
    public bool IsOnline { get; set; } = false;
    public DateTime? LastOnline { get; set; }
    public string? InstagramHandle { get; set; }
    public string? FacebookId { get; set; }
    public string? SpotifyId { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool MessageNotifications { get; set; } = true;
    public bool MatchNotifications { get; set; } = true;
    public bool LikeNotifications { get; set; } = true;
    public bool SuperLikeNotifications { get; set; } = true;
    public string? SubscriptionType { get; set; } = "Free";
    public DateTime? SubscriptionExpiresAt { get; set; }
    public bool HasPremium { get; set; } = false;
    public int SuperLikesRemaining { get; set; } = 5;
    public DateTime? SuperLikesResetAt { get; set; }
    public int BoostsRemaining { get; set; } = 0;
    public DateTime? LastBoostAt { get; set; }
    public bool IsBoostActive { get; set; } = false;
    public DateTime? BoostExpiresAt { get; set; }
    public string? DeviceType { get; set; }
    public string? AppVersion { get; set; }
    public string? Language { get; set; } = "en";
    public string? Timezone { get; set; }
    public int ProfileViews { get; set; } = 0;
    public int LikesReceived { get; set; } = 0;
    public int MatchesCount { get; set; } = 0;
    public DateTime? DeletedAt { get; set; }
    public string? DeleteReason { get; set; }

    // Calculated properties
    public int Age => DateOfBirth.HasValue ? DateTime.Today.Year - DateOfBirth.Value.Year - 
        (DateTime.Today.DayOfYear < DateOfBirth.Value.DayOfYear ? 1 : 0) : 0;

    public bool IsPremiumActive => HasPremium && SubscriptionExpiresAt.HasValue && 
        SubscriptionExpiresAt.Value > DateTime.UtcNow;

    public TimeSpan TimeSinceLastActive => DateTime.UtcNow - LastActive;

    public bool IsRecentlyActive => TimeSinceLastActive.TotalHours < 24;

    // Navigation properties would be defined in separate entities
    // to maintain clean separation and avoid circular references
}