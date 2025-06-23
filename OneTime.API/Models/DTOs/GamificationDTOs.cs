namespace OneTime.API.Models.DTOs;

// Request DTOs
public class ClaimRewardRequest
{
    public string RewardId { get; set; } = string.Empty;
}

public class ShareAchievementRequest
{
    public string AchievementId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // facebook, twitter, instagram
}

// Response DTOs
public class GamificationProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public int Level { get; set; }
    public int TotalXP { get; set; }
    public int CurrentLevelXP { get; set; }
    public int NextLevelXP { get; set; }
    public int XPToNextLevel { get; set; }
    public List<BadgeResponse> Badges { get; set; } = new();
    public List<StreakResponse> ActiveStreaks { get; set; } = new();
    public int WeeklyRank { get; set; }
    public int MonthlyRank { get; set; }
    public int AllTimeRank { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class LeaderboardResponse
{
    public string Type { get; set; } = string.Empty; // weekly, monthly, all-time
    public List<LeaderboardEntryResponse> Entries { get; set; } = new();
    public LeaderboardEntryResponse? UserEntry { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class LeaderboardEntryResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserPhotoUrl { get; set; }
    public int Rank { get; set; }
    public int Score { get; set; }
    public int Level { get; set; }
    public List<string> TopBadges { get; set; } = new();
}

public class AchievementResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Rarity { get; set; } = string.Empty; // common, rare, epic, legendary
    public int XPReward { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public bool IsUnlocked { get; set; }
    public int Progress { get; set; }
    public int MaxProgress { get; set; }
}

public class BadgeResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Rarity { get; set; } = string.Empty;
    public DateTime? EarnedAt { get; set; }
    public bool IsEarned { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class StreakResponse
{
    public string Type { get; set; } = string.Empty; // daily_login, messaging, etc.
    public string Name { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateTime LastActivity { get; set; }
    public bool IsActive { get; set; }
    public int XPPerDay { get; set; }
}

public class DailyCheckInResponse
{
    public bool Success { get; set; }
    public int XPEarned { get; set; }
    public int CurrentStreak { get; set; }
    public List<BadgeResponse> BadgesEarned { get; set; } = new();
    public List<AchievementResponse> AchievementsUnlocked { get; set; } = new();
    public RewardResponse? StreakReward { get; set; }
}

public class RewardResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // xp, badge, premium_feature, super_like, boost
    public int Value { get; set; }
    public string? Icon { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsClaimed { get; set; }
}

public class ChallengeResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // daily, weekly, monthly, special
    public int Progress { get; set; }
    public int Target { get; set; }
    public List<RewardResponse> Rewards { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsParticipating { get; set; }
}

public class LevelProgressResponse
{
    public int CurrentLevel { get; set; }
    public int CurrentXP { get; set; }
    public int XPForCurrentLevel { get; set; }
    public int XPForNextLevel { get; set; }
    public double ProgressPercentage { get; set; }
    public List<LevelRewardResponse> UpcomingRewards { get; set; } = new();
}

public class LevelRewardResponse
{
    public int Level { get; set; }
    public string RewardType { get; set; } = string.Empty;
    public string RewardDescription { get; set; } = string.Empty;
    public string? Icon { get; set; }
}

public class ExperienceHistoryResponse
{
    public DateTime Date { get; set; }
    public string Activity { get; set; } = string.Empty;
    public int XPEarned { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ShareResponse
{
    public string ShareUrl { get; set; } = string.Empty;
    public string ShareText { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class GamificationDashboardResponse
{
    public GamificationProfileResponse Profile { get; set; } = new();
    public List<AchievementResponse> RecentAchievements { get; set; } = new();
    public List<ChallengeResponse> ActiveChallenges { get; set; } = new();
    public List<RewardResponse> AvailableRewards { get; set; } = new();
    public DailyCheckInStatus DailyCheckIn { get; set; } = new();
}

public class DailyCheckInStatus
{
    public bool HasCheckedInToday { get; set; }
    public int CurrentStreak { get; set; }
    public int XPReward { get; set; }
    public RewardResponse? StreakBonus { get; set; }
}