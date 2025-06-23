namespace OneTime.API.Models.DTOs;

// Request DTOs
public class LikeProfileRequest
{
    public string TargetUserId { get; set; } = string.Empty;
}

public class PassProfileRequest
{
    public string TargetUserId { get; set; } = string.Empty;
}

public class SuperLikeProfileRequest
{
    public string TargetUserId { get; set; } = string.Empty;
}

public class UpdatePreferencesRequest
{
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MaxDistance { get; set; }
    public string? InterestedIn { get; set; }
    public bool? OnlyVerifiedProfiles { get; set; }
    public List<string>? InterestedInInterests { get; set; }
}

public class BlockUserRequest
{
    public string TargetUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class ReportUserRequest
{
    public string TargetUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

// Response DTOs
public class MatchResponse
{
    public bool IsMatch { get; set; }
    public string? MatchId { get; set; }
    public DateTime? MatchedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ConversationId { get; set; }
    public UserProfileResponse? UserProfile { get; set; }
    public MessageResponse? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}

public class MatchingPreferencesResponse
{
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public int MaxDistance { get; set; }
    public string InterestedIn { get; set; } = string.Empty;
    public bool OnlyVerifiedProfiles { get; set; }
    public List<string> InterestedInInterests { get; set; } = new();
}

public class CompatibilityResponse
{
    public double Score { get; set; }
    public List<CompatibilityFactor> Factors { get; set; } = new();
}

public class CompatibilityFactor
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Description { get; set; } = string.Empty;
}