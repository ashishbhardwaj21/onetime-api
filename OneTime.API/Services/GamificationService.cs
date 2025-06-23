using Microsoft.EntityFrameworkCore;
using OneTime.API.Data;
using OneTime.API.Models;
using OneTime.API.Models.DTOs;
using OneTime.API.Models.Entities;

namespace OneTime.API.Services;

public interface IGamificationService
{
    Task<ServiceResult<bool>> AwardXPAsync(string userId, string activity, int xpAmount);
    Task<ServiceResult<GamificationProfileResponse>> GetGamificationProfileAsync(string userId);
    Task<ServiceResult<LeaderboardResponse>> GetLeaderboardAsync(string type, int limit);
    Task<ServiceResult<List<AchievementResponse>>> GetUserAchievementsAsync(string userId);
    Task<ServiceResult<List<BadgeResponse>>> GetUserBadgesAsync(string userId);
    Task<ServiceResult<List<BadgeResponse>>> GetAvailableBadgesAsync();
    Task<ServiceResult<List<StreakResponse>>> GetUserStreaksAsync(string userId);
    Task<ServiceResult<DailyCheckInResponse>> ProcessDailyCheckInAsync(string userId);
    Task<ServiceResult<RewardResponse>> ClaimRewardAsync(string userId, string rewardId);
    Task<ServiceResult<List<ChallengeResponse>>> GetActiveChallengesAsync(string userId);
    Task<ServiceResult<bool>> ParticipateInChallengeAsync(string userId, string challengeId);
    Task<ServiceResult<LevelProgressResponse>> GetLevelProgressAsync(string userId);
    Task<ServiceResult<List<RewardResponse>>> GetAvailableRewardsAsync(string userId);
    Task<ServiceResult<List<ExperienceHistoryResponse>>> GetExperienceHistoryAsync(string userId, int days);
    Task<ServiceResult<ShareResponse>> ShareAchievementAsync(string userId, string achievementId, string platform);
    Task<ServiceResult<GamificationDashboardResponse>> GetGamificationDashboardAsync(string userId);
}

public class GamificationService : IGamificationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<GamificationService> _logger;

    public GamificationService(
        ApplicationDbContext context,
        INotificationService notificationService,
        IAnalyticsService analyticsService,
        ILogger<GamificationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<ServiceResult<bool>> AwardXPAsync(string userId, string activity, int xpAmount)
    {
        try
        {
            var profile = await GetOrCreateGamificationProfileAsync(userId);
            var oldLevel = CalculateLevel(profile.TotalXP);
            
            // Award XP
            profile.TotalXP += xpAmount;
            profile.WeeklyXP += xpAmount;
            profile.MonthlyXP += xpAmount;
            profile.LastActivityDate = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            // Record experience history
            var experienceRecord = new ExperienceHistory
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Activity = activity,
                XPEarned = xpAmount,
                Description = GetActivityDescription(activity),
                CreatedAt = DateTime.UtcNow
            };

            _context.ExperienceHistory.Add(experienceRecord);

            var newLevel = CalculateLevel(profile.TotalXP);
            
            // Check for level up
            if (newLevel > oldLevel)
            {
                profile.Level = newLevel;
                await ProcessLevelUpAsync(userId, newLevel);
            }

            // Check for achievements
            await CheckAndAwardAchievementsAsync(userId, activity);

            await _context.SaveChangesAsync();

            // Track analytics
            await _analyticsService.TrackEventAsync(userId, "xp_awarded", new Dictionary<string, object>
            {
                {"activity", activity},
                {"xp_amount", xpAmount},
                {"total_xp", profile.TotalXP},
                {"level", profile.Level}
            });

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding XP to user {UserId}", userId);
            return ServiceResult<bool>.Failure("An error occurred while awarding XP");
        }
    }

    public async Task<ServiceResult<GamificationProfileResponse>> GetGamificationProfileAsync(string userId)
    {
        try
        {
            var profile = await GetOrCreateGamificationProfileAsync(userId);
            var userBadges = await _context.UserBadges
                .Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId)
                .OrderByDescending(ub => ub.EarnedAt)
                .Take(5)
                .ToListAsync();

            var currentLevelXP = GetXPForLevel(profile.Level);
            var nextLevelXP = GetXPForLevel(profile.Level + 1);

            var response = new GamificationProfileResponse
            {
                UserId = userId,
                Level = profile.Level,
                TotalXP = profile.TotalXP,
                CurrentLevelXP = currentLevelXP,
                NextLevelXP = nextLevelXP,
                XPToNextLevel = nextLevelXP - profile.TotalXP,
                Badges = userBadges.Select(ub => new BadgeResponse
                {
                    Id = ub.Badge.Id,
                    Name = ub.Badge.Name,
                    Description = ub.Badge.Description,
                    Icon = ub.Badge.Icon,
                    Rarity = ub.Badge.Rarity,
                    EarnedAt = ub.EarnedAt,
                    IsEarned = true,
                    Category = ub.Badge.Category
                }).ToList(),
                ActiveStreaks = await GetActiveStreaksAsync(userId),
                WeeklyRank = await GetUserRankAsync(userId, "weekly"),
                MonthlyRank = await GetUserRankAsync(userId, "monthly"),
                AllTimeRank = await GetUserRankAsync(userId, "all-time"),
                LastUpdated = profile.UpdatedAt ?? profile.CreatedAt
            };

            return ServiceResult<GamificationProfileResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gamification profile for user {UserId}", userId);
            return ServiceResult<GamificationProfileResponse>.Failure("An error occurred while getting gamification profile");
        }
    }

    public async Task<ServiceResult<LeaderboardResponse>> GetLeaderboardAsync(string type, int limit)
    {
        try
        {
            IQueryable<GamificationProfile> query = _context.GamificationProfiles
                .Include(gp => gp.User)
                .Include(gp => gp.UserBadges)
                .ThenInclude(ub => ub.Badge);

            query = type.ToLower() switch
            {
                "weekly" => query.OrderByDescending(gp => gp.WeeklyXP),
                "monthly" => query.OrderByDescending(gp => gp.MonthlyXP),
                _ => query.OrderByDescending(gp => gp.TotalXP)
            };

            var profiles = await query.Take(limit).ToListAsync();

            var entries = profiles.Select((profile, index) => new LeaderboardEntryResponse
            {
                UserId = profile.UserId,
                UserName = profile.User.FirstName,
                Rank = index + 1,
                Score = type.ToLower() switch
                {
                    "weekly" => profile.WeeklyXP,
                    "monthly" => profile.MonthlyXP,
                    _ => profile.TotalXP
                },
                Level = profile.Level,
                TopBadges = profile.UserBadges
                    .OrderByDescending(ub => ub.EarnedAt)
                    .Take(3)
                    .Select(ub => ub.Badge.Name)
                    .ToList()
            }).ToList();

            var response = new LeaderboardResponse
            {
                Type = type,
                Entries = entries,
                LastUpdated = DateTime.UtcNow
            };

            return ServiceResult<LeaderboardResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard for type {Type}", type);
            return ServiceResult<LeaderboardResponse>.Failure("An error occurred while getting leaderboard");
        }
    }

    public async Task<ServiceResult<List<BadgeResponse>>> GetUserBadgesAsync(string userId)
    {
        try
        {
            var userBadges = await _context.UserBadges
                .Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId)
                .OrderByDescending(ub => ub.EarnedAt)
                .ToListAsync();

            var response = userBadges.Select(ub => new BadgeResponse
            {
                Id = ub.Badge.Id,
                Name = ub.Badge.Name,
                Description = ub.Badge.Description,
                Icon = ub.Badge.Icon,
                Rarity = ub.Badge.Rarity,
                EarnedAt = ub.EarnedAt,
                IsEarned = true,
                Category = ub.Badge.Category
            }).ToList();

            return ServiceResult<List<BadgeResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user badges for {UserId}", userId);
            return ServiceResult<List<BadgeResponse>>.Failure("An error occurred while getting user badges");
        }
    }

    public async Task<ServiceResult<List<BadgeResponse>>> GetAvailableBadgesAsync()
    {
        try
        {
            var badges = await _context.Badges
                .OrderBy(b => b.Category)
                .ThenBy(b => b.Name)
                .ToListAsync();

            var response = badges.Select(b => new BadgeResponse
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Icon = b.Icon,
                Rarity = b.Rarity,
                IsEarned = false,
                Category = b.Category
            }).ToList();

            return ServiceResult<List<BadgeResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available badges");
            return ServiceResult<List<BadgeResponse>>.Failure("An error occurred while getting available badges");
        }
    }

    public async Task<ServiceResult<DailyCheckInResponse>> ProcessDailyCheckInAsync(string userId)
    {
        try
        {
            var profile = await GetOrCreateGamificationProfileAsync(userId);
            var today = DateTime.UtcNow.Date;
            var lastLogin = profile.LastLoginDate?.Date;

            // Check if already checked in today
            if (lastLogin == today)
            {
                return ServiceResult<DailyCheckInResponse>.Failure("Already checked in today");
            }

            // Calculate streak
            var isConsecutive = lastLogin == today.AddDays(-1);
            if (isConsecutive)
            {
                profile.DailyLoginStreak++;
            }
            else
            {
                profile.DailyLoginStreak = 1;
            }

            profile.LastLoginDate = DateTime.UtcNow;

            // Calculate XP reward (base 10 + streak bonus)
            var baseXP = 10;
            var streakBonus = Math.Min(profile.DailyLoginStreak * 2, 20); // Max 20 bonus XP
            var totalXP = baseXP + streakBonus;

            // Award XP
            await AwardXPAsync(userId, "daily_check_in", totalXP);

            // Check for streak rewards
            RewardResponse? streakReward = null;
            if (profile.DailyLoginStreak % 7 == 0) // Weekly milestone
            {
                // Award streak reward (e.g., super like)
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.SuperLikesRemaining += 1;
                    streakReward = new RewardResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Weekly Streak Bonus",
                        Description = "1 Super Like for 7-day streak!",
                        Type = "super_like",
                        Value = 1
                    };
                }
            }

            await _context.SaveChangesAsync();

            var response = new DailyCheckInResponse
            {
                Success = true,
                XPEarned = totalXP,
                CurrentStreak = profile.DailyLoginStreak,
                StreakReward = streakReward
            };

            return ServiceResult<DailyCheckInResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing daily check-in for user {UserId}", userId);
            return ServiceResult<DailyCheckInResponse>.Failure("An error occurred during check-in");
        }
    }

    // Helper methods
    private async Task<GamificationProfile> GetOrCreateGamificationProfileAsync(string userId)
    {
        var profile = await _context.GamificationProfiles
            .Include(gp => gp.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .FirstOrDefaultAsync(gp => gp.UserId == userId);

        if (profile == null)
        {
            profile = new GamificationProfile
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Level = 1,
                TotalXP = 0,
                WeeklyXP = 0,
                MonthlyXP = 0,
                DailyLoginStreak = 0,
                MessagingStreak = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.GamificationProfiles.Add(profile);
        }

        return profile;
    }

    private static int CalculateLevel(int totalXP)
    {
        // Level formula: Each level requires progressively more XP
        // Level 1: 0 XP, Level 2: 100 XP, Level 3: 250 XP, etc.
        var level = 1;
        var xpRequired = 0;
        var increment = 100;

        while (totalXP >= xpRequired + increment)
        {
            xpRequired += increment;
            level++;
            increment = (int)(increment * 1.5); // Increase by 50% each level
        }

        return level;
    }

    private static int GetXPForLevel(int level)
    {
        if (level <= 1) return 0;

        var xpRequired = 0;
        var increment = 100;

        for (var i = 2; i <= level; i++)
        {
            xpRequired += increment;
            increment = (int)(increment * 1.5);
        }

        return xpRequired;
    }

    private async Task ProcessLevelUpAsync(string userId, int newLevel)
    {
        try
        {
            // Send level up notification
            await _notificationService.SendNotificationAsync(userId, new NotificationRequest
            {
                Title = "Level Up! 🎉",
                Body = $"Congratulations! You've reached level {newLevel}!",
                Type = "level_up",
                Data = new Dictionary<string, object>
                {
                    {"new_level", newLevel}
                }
            });

            // Award level rewards
            if (newLevel % 5 == 0) // Every 5 levels
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.SuperLikesRemaining += 2;
                    user.BoostsRemaining += 1;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing level up for user {UserId}", userId);
        }
    }

    private async Task CheckAndAwardAchievementsAsync(string userId, string activity)
    {
        try
        {
            // Get user's current stats
            var profile = await _context.GamificationProfiles
                .Include(gp => gp.UserBadges)
                .FirstOrDefaultAsync(gp => gp.UserId == userId);

            if (profile == null) return;

            var badgesToAward = new List<string>();

            // Check activity-specific achievements
            switch (activity)
            {
                case "got_match":
                    var matchCount = await _context.Matches
                        .CountAsync(m => m.User1Id == userId || m.User2Id == userId);
                    
                    if (matchCount == 1 && !HasBadge(profile, "First Match"))
                        badgesToAward.Add("First Match");
                    
                    if (matchCount >= 10 && !HasBadge(profile, "Social Butterfly"))
                        badgesToAward.Add("Social Butterfly");
                    break;

                case "sent_message":
                    var messageCount = await _context.Messages
                        .CountAsync(m => m.SenderId == userId);
                    
                    if (messageCount == 1 && !HasBadge(profile, "Conversation Starter"))
                        badgesToAward.Add("Conversation Starter");
                    
                    if (messageCount >= 100 && !HasBadge(profile, "Chatterbox"))
                        badgesToAward.Add("Chatterbox");
                    break;

                case "used_super_like":
                    if (!HasBadge(profile, "Super Star"))
                        badgesToAward.Add("Super Star");
                    break;
            }

            // Award badges
            foreach (var badgeName in badgesToAward)
            {
                await AwardBadgeAsync(userId, badgeName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking achievements for user {UserId}", userId);
        }
    }

    private async Task AwardBadgeAsync(string userId, string badgeName)
    {
        try
        {
            var badge = await _context.Badges
                .FirstOrDefaultAsync(b => b.Name == badgeName);

            if (badge == null) return;

            var userBadge = new UserBadge
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                BadgeId = badge.Id,
                EarnedAt = DateTime.UtcNow
            };

            _context.UserBadges.Add(userBadge);

            // Send notification
            await _notificationService.SendNotificationAsync(userId, new NotificationRequest
            {
                Title = "Badge Earned! 🏆",
                Body = $"You've earned the '{badgeName}' badge!",
                Type = "badge_earned",
                Data = new Dictionary<string, object>
                {
                    {"badge_name", badgeName},
                    {"badge_id", badge.Id}
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding badge {BadgeName} to user {UserId}", badgeName, userId);
        }
    }

    private static bool HasBadge(GamificationProfile profile, string badgeName)
    {
        return profile.UserBadges?.Any(ub => ub.Badge.Name == badgeName) ?? false;
    }

    private async Task<List<StreakResponse>> GetActiveStreaksAsync(string userId)
    {
        var profile = await _context.GamificationProfiles
            .FirstOrDefaultAsync(gp => gp.UserId == userId);

        if (profile == null) return new List<StreakResponse>();

        return new List<StreakResponse>
        {
            new()
            {
                Type = "daily_login",
                Name = "Daily Login",
                CurrentStreak = profile.DailyLoginStreak,
                BestStreak = profile.DailyLoginStreak, // We'd need to track this separately
                LastActivity = profile.LastLoginDate ?? DateTime.UtcNow,
                IsActive = profile.LastLoginDate?.Date == DateTime.UtcNow.Date,
                XPPerDay = 10
            },
            new()
            {
                Type = "messaging",
                Name = "Daily Messaging",
                CurrentStreak = profile.MessagingStreak,
                BestStreak = profile.MessagingStreak,
                LastActivity = profile.LastActivityDate ?? DateTime.UtcNow,
                IsActive = profile.LastActivityDate?.Date == DateTime.UtcNow.Date,
                XPPerDay = 5
            }
        };
    }

    private async Task<int> GetUserRankAsync(string userId, string type)
    {
        IQueryable<GamificationProfile> query = _context.GamificationProfiles;

        query = type.ToLower() switch
        {
            "weekly" => query.OrderByDescending(gp => gp.WeeklyXP),
            "monthly" => query.OrderByDescending(gp => gp.MonthlyXP),
            _ => query.OrderByDescending(gp => gp.TotalXP)
        };

        var userIds = await query.Select(gp => gp.UserId).ToListAsync();
        var rank = userIds.IndexOf(userId) + 1;
        
        return rank > 0 ? rank : 0;
    }

    private static string GetActivityDescription(string activity)
    {
        return activity switch
        {
            "got_match" => "Got a match",
            "liked_profile" => "Liked a profile",
            "sent_message" => "Sent a message",
            "daily_check_in" => "Daily check-in",
            "used_super_like" => "Used a Super Like",
            "completed_profile" => "Completed profile",
            _ => "Activity completed"
        };
    }

    // Implement remaining interface methods with NotImplementedException for now
    public async Task<ServiceResult<List<AchievementResponse>>> GetUserAchievementsAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<List<StreakResponse>>> GetUserStreaksAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<RewardResponse>> ClaimRewardAsync(string userId, string rewardId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<List<ChallengeResponse>>> GetActiveChallengesAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> ParticipateInChallengeAsync(string userId, string challengeId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<LevelProgressResponse>> GetLevelProgressAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<List<RewardResponse>>> GetAvailableRewardsAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<List<ExperienceHistoryResponse>>> GetExperienceHistoryAsync(string userId, int days)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<ShareResponse>> ShareAchievementAsync(string userId, string achievementId, string platform)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<GamificationDashboardResponse>> GetGamificationDashboardAsync(string userId)
    {
        throw new NotImplementedException();
    }
}