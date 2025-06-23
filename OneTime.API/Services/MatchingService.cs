using Microsoft.EntityFrameworkCore;
using OneTime.API.Data;
using OneTime.API.Models;
using OneTime.API.Models.DTOs;
using OneTime.API.Models.Entities;

namespace OneTime.API.Services;

public interface IMatchingService
{
    Task<ServiceResult<List<UserProfileResponse>>> DiscoverProfilesAsync(string userId, int count = 10);
    Task<ServiceResult<MatchResponse>> LikeProfileAsync(string userId, string targetUserId);
    Task<ServiceResult<bool>> PassProfileAsync(string userId, string targetUserId);
    Task<ServiceResult<MatchResponse>> SuperLikeProfileAsync(string userId, string targetUserId);
    Task<ServiceResult<List<MatchResponse>>> GetMatchesAsync(string userId);
    Task<ServiceResult<bool>> UnmatchAsync(string userId, string matchId);
    Task<ServiceResult<MatchingPreferencesResponse>> GetPreferencesAsync(string userId);
    Task<ServiceResult<bool>> UpdatePreferencesAsync(string userId, UpdatePreferencesRequest request);
    Task<ServiceResult<bool>> ActivateBoostAsync(string userId);
    Task<ServiceResult<bool>> BlockUserAsync(string userId, string targetUserId, string reason);
    Task<ServiceResult<bool>> ReportUserAsync(string userId, string targetUserId, string reason, string details);
    Task<ServiceResult<List<UserProfileResponse>>> GetLikedMeAsync(string userId);
    Task<ServiceResult<CompatibilityResponse>> GetCompatibilityScoreAsync(string userId, string targetUserId);
}

public class MatchingService : IMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly IGamificationService _gamificationService;
    private readonly INotificationService _notificationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IAIService _aiService;
    private readonly ILogger<MatchingService> _logger;

    public MatchingService(
        ApplicationDbContext context,
        IGamificationService gamificationService,
        INotificationService notificationService,
        IAnalyticsService analyticsService,
        IAIService aiService,
        ILogger<MatchingService> logger)
    {
        _context = context;
        _gamificationService = gamificationService;
        _notificationService = notificationService;
        _analyticsService = analyticsService;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<ServiceResult<List<UserProfileResponse>>> DiscoverProfilesAsync(string userId, int count = 10)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ServiceResult<List<UserProfileResponse>>.Failure("User not found");
            }

            // Get user's preferences
            var preferences = await GetUserPreferencesAsync(userId);
            
            // Get users that have been liked, passed, or blocked
            var excludedUserIds = await GetExcludedUserIdsAsync(userId);
            
            // Build query for potential matches
            var query = _context.Users
                .Where(u => u.Id != userId && 
                           !excludedUserIds.Contains(u.Id) &&
                           u.IsActive && 
                           !u.IsBlocked &&
                           u.ShowMeOnDiscovery)
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.Photos)
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.UserInterests)
                .ThenInclude(ui => ui.Interest);

            // Apply age filter
            if (preferences.MinAge.HasValue)
            {
                query = query.Where(u => u.DateOfBirth <= DateTime.Today.AddYears(-preferences.MinAge.Value));
            }
            
            if (preferences.MaxAge.HasValue)
            {
                query = query.Where(u => u.DateOfBirth >= DateTime.Today.AddYears(-preferences.MaxAge.Value));
            }

            // Apply gender filter
            if (!string.IsNullOrEmpty(preferences.InterestedIn) && preferences.InterestedIn != "Everyone")
            {
                query = query.Where(u => u.Gender == preferences.InterestedIn);
            }

            // Apply distance filter if location is available
            if (user.Latitude.HasValue && user.Longitude.HasValue && preferences.MaxDistance.HasValue)
            {
                var userLat = user.Latitude.Value;
                var userLon = user.Longitude.Value;
                var maxDistance = preferences.MaxDistance.Value;

                query = query.Where(u => u.Latitude.HasValue && u.Longitude.HasValue)
                           .Where(u => CalculateDistance(userLat, userLon, u.Latitude!.Value, u.Longitude!.Value) <= maxDistance);
            }

            // Get potential matches
            var potentialMatches = await query
                .OrderBy(u => Guid.NewGuid()) // Random ordering
                .Take(count * 3) // Get more than needed for filtering
                .ToListAsync();

            // Calculate compatibility scores and rank
            var scoredMatches = new List<(ApplicationUser user, double score)>();
            
            foreach (var match in potentialMatches)
            {
                var compatibilityScore = await CalculateCompatibilityScoreAsync(user, match);
                scoredMatches.Add((match, compatibilityScore));
            }

            // Sort by compatibility score and take requested count
            var topMatches = scoredMatches
                .OrderByDescending(x => x.score)
                .Take(count)
                .Select(x => x.user)
                .ToList();

            // Convert to response DTOs
            var profileResponses = topMatches.Select(match => new UserProfileResponse
            {
                Id = match.Id,
                Name = match.UserProfile?.FullName ?? "User",
                Age = match.Age,
                Bio = match.Bio,
                Photos = match.UserProfile?.Photos?.OrderBy(p => p.Order)
                    .Select(p => new PhotoResponse
                    {
                        Id = p.Id,
                        Url = p.Url,
                        Order = p.Order,
                        IsMain = p.IsMain
                    }).ToList() ?? new List<PhotoResponse>(),
                Interests = match.UserProfile?.UserInterests?.Select(ui => ui.Interest.Name).ToList() ?? new List<string>(),
                Distance = user.Latitude.HasValue && user.Longitude.HasValue && 
                          match.Latitude.HasValue && match.Longitude.HasValue
                    ? CalculateDistance(user.Latitude.Value, user.Longitude.Value, 
                                      match.Latitude.Value, match.Longitude.Value)
                    : null,
                IsVerified = match.IsVerified,
                LastActive = match.LastActive,
                Occupation = match.Occupation,
                Education = match.Education,
                Height = match.Height,
                Drinking = match.Drinking,
                Smoking = match.Smoking,
                Children = match.Children
            }).ToList();

            // Track discovery analytics
            await _analyticsService.TrackEventAsync(userId, "profiles_discovered", new Dictionary<string, object>
            {
                {"count", profileResponses.Count},
                {"timestamp", DateTime.UtcNow}
            });

            return ServiceResult<List<UserProfileResponse>>.Success(profileResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering profiles for user {UserId}", userId);
            return ServiceResult<List<UserProfileResponse>>.Failure("An error occurred while discovering profiles");
        }
    }

    public async Task<ServiceResult<MatchResponse>> LikeProfileAsync(string userId, string targetUserId)
    {
        try
        {
            // Check if already liked
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.LikerId == userId && l.LikedId == targetUserId);
            
            if (existingLike != null)
            {
                return ServiceResult<MatchResponse>.Failure("Profile already liked");
            }

            // Create like record
            var like = new Like
            {
                Id = Guid.NewGuid().ToString(),
                LikerId = userId,
                LikedId = targetUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Likes.Add(like);

            // Check if it's a mutual like (match)
            var reciprocalLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.LikerId == targetUserId && l.LikedId == userId);

            bool isMatch = reciprocalLike != null;
            Match? match = null;

            if (isMatch)
            {
                // Create match
                match = new Match
                {
                    Id = Guid.NewGuid().ToString(),
                    User1Id = userId,
                    User2Id = targetUserId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7), // Matches expire after 7 days
                    IsActive = true
                };

                _context.Matches.Add(match);

                // Create conversation for the match
                var conversation = new Conversation
                {
                    Id = Guid.NewGuid().ToString(),
                    MatchId = match.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Conversations.Add(conversation);

                // Award XP for getting a match
                await _gamificationService.AwardXPAsync(userId, "got_match", 25);
                await _gamificationService.AwardXPAsync(targetUserId, "got_match", 25);

                // Send match notifications
                await _notificationService.SendMatchNotificationAsync(userId, targetUserId);
                await _notificationService.SendMatchNotificationAsync(targetUserId, userId);

                // Track match analytics
                await _analyticsService.TrackEventAsync(userId, "match_created", new Dictionary<string, object>
                {
                    {"target_user_id", targetUserId},
                    {"method", "mutual_like"}
                });
            }
            else
            {
                // Award XP for liking a profile
                await _gamificationService.AwardXPAsync(userId, "liked_profile", 2);

                // Send like notification to target user
                await _notificationService.SendLikeNotificationAsync(targetUserId, userId);
            }

            await _context.SaveChangesAsync();

            var response = new MatchResponse
            {
                IsMatch = isMatch,
                MatchId = match?.Id,
                MatchedAt = match?.CreatedAt,
                ConversationId = match != null ? 
                    (await _context.Conversations.FirstOrDefaultAsync(c => c.MatchId == match.Id))?.Id : null
            };

            return ServiceResult<MatchResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking profile for user {UserId}, target {TargetUserId}", userId, targetUserId);
            return ServiceResult<MatchResponse>.Failure("An error occurred while liking the profile");
        }
    }

    public async Task<ServiceResult<bool>> PassProfileAsync(string userId, string targetUserId)
    {
        try
        {
            // Check if already passed
            var existingPass = await _context.Passes
                .FirstOrDefaultAsync(p => p.PasserId == userId && p.PassedId == targetUserId);
            
            if (existingPass != null)
            {
                return ServiceResult<bool>.Success(true);
            }

            // Create pass record
            var pass = new Pass
            {
                Id = Guid.NewGuid().ToString(),
                PasserId = userId,
                PassedId = targetUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Passes.Add(pass);
            await _context.SaveChangesAsync();

            // Track analytics
            await _analyticsService.TrackEventAsync(userId, "profile_passed", new Dictionary<string, object>
            {
                {"target_user_id", targetUserId}
            });

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error passing profile for user {UserId}, target {TargetUserId}", userId, targetUserId);
            return ServiceResult<bool>.Failure("An error occurred while passing the profile");
        }
    }

    public async Task<ServiceResult<MatchResponse>> SuperLikeProfileAsync(string userId, string targetUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ServiceResult<MatchResponse>.Failure("User not found");
            }

            // Check if user has super likes remaining
            if (user.SuperLikesRemaining <= 0)
            {
                return ServiceResult<MatchResponse>.Failure("No super likes remaining");
            }

            // Check if already super liked
            var existingSuperLike = await _context.SuperLikes
                .FirstOrDefaultAsync(sl => sl.SuperLikerId == userId && sl.SuperLikedId == targetUserId);
            
            if (existingSuperLike != null)
            {
                return ServiceResult<MatchResponse>.Failure("Profile already super liked");
            }

            // Decrement super likes
            user.SuperLikesRemaining--;
            
            // Create super like record
            var superLike = new SuperLike
            {
                Id = Guid.NewGuid().ToString(),
                SuperLikerId = userId,
                SuperLikedId = targetUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SuperLikes.Add(superLike);

            // Check if target user has liked this user (instant match with super like)
            var reciprocalLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.LikerId == targetUserId && l.LikedId == userId);

            bool isMatch = reciprocalLike != null;
            Match? match = null;

            if (isMatch)
            {
                // Create match
                match = new Match
                {
                    Id = Guid.NewGuid().ToString(),
                    User1Id = userId,
                    User2Id = targetUserId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    IsActive = true,
                    IsSuperLikeMatch = true
                };

                _context.Matches.Add(match);

                // Create conversation
                var conversation = new Conversation
                {
                    Id = Guid.NewGuid().ToString(),
                    MatchId = match.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Conversations.Add(conversation);

                // Award XP for super like match
                await _gamificationService.AwardXPAsync(userId, "got_match", 50); // More XP for super like match
                await _gamificationService.AwardXPAsync(targetUserId, "got_match", 25);

                // Send match notifications
                await _notificationService.SendMatchNotificationAsync(userId, targetUserId);
                await _notificationService.SendMatchNotificationAsync(targetUserId, userId);
            }
            else
            {
                // Award XP for using super like
                await _gamificationService.AwardXPAsync(userId, "used_super_like", 15);

                // Send super like notification
                await _notificationService.SendSuperLikeNotificationAsync(targetUserId, userId);
            }

            await _context.SaveChangesAsync();

            var response = new MatchResponse
            {
                IsMatch = isMatch,
                MatchId = match?.Id,
                MatchedAt = match?.CreatedAt,
                ConversationId = match != null ? 
                    (await _context.Conversations.FirstOrDefaultAsync(c => c.MatchId == match.Id))?.Id : null
            };

            return ServiceResult<MatchResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error super liking profile for user {UserId}, target {TargetUserId}", userId, targetUserId);
            return ServiceResult<MatchResponse>.Failure("An error occurred while super liking the profile");
        }
    }

    public async Task<ServiceResult<List<MatchResponse>>> GetMatchesAsync(string userId)
    {
        try
        {
            var matches = await _context.Matches
                .Where(m => (m.User1Id == userId || m.User2Id == userId) && 
                           m.IsActive && 
                           m.ExpiresAt > DateTime.UtcNow)
                .Include(m => m.User1)
                .Include(m => m.User2)
                .Include(m => m.Conversation)
                .ThenInclude(c => c.Messages.OrderByDescending(msg => msg.CreatedAt).Take(1))
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var matchResponses = matches.Select(match =>
            {
                var otherUser = match.User1Id == userId ? match.User2 : match.User1;
                var lastMessage = match.Conversation?.Messages?.FirstOrDefault();

                return new MatchResponse
                {
                    MatchId = match.Id,
                    IsMatch = true,
                    MatchedAt = match.CreatedAt,
                    ExpiresAt = match.ExpiresAt,
                    ConversationId = match.Conversation?.Id,
                    UserProfile = new UserProfileResponse
                    {
                        Id = otherUser.Id,
                        Name = otherUser.UserProfile?.FullName ?? "User",
                        Age = otherUser.Age,
                        Photos = otherUser.UserProfile?.Photos?.OrderBy(p => p.Order)
                            .Select(p => new PhotoResponse
                            {
                                Id = p.Id,
                                Url = p.Url,
                                Order = p.Order,
                                IsMain = p.IsMain
                            }).Take(1).ToList() ?? new List<PhotoResponse>(),
                        IsVerified = otherUser.IsVerified,
                        LastActive = otherUser.LastActive
                    },
                    LastMessage = lastMessage != null ? new MessageResponse
                    {
                        Id = lastMessage.Id,
                        Content = lastMessage.Content,
                        Type = lastMessage.Type,
                        CreatedAt = lastMessage.CreatedAt,
                        SenderId = lastMessage.SenderId
                    } : null,
                    UnreadCount = match.Conversation?.Messages?
                        .Count(m => m.SenderId != userId && 
                                   !m.MessageReads.Any(mr => mr.UserId == userId)) ?? 0
                };
            }).ToList();

            return ServiceResult<List<MatchResponse>>.Success(matchResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matches for user {UserId}", userId);
            return ServiceResult<List<MatchResponse>>.Failure("An error occurred while getting matches");
        }
    }

    public async Task<ServiceResult<bool>> UnmatchAsync(string userId, string matchId)
    {
        try
        {
            var match = await _context.Matches
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == matchId && 
                                        (m.User1Id == userId || m.User2Id == userId));

            if (match == null)
            {
                return ServiceResult<bool>.Failure("Match not found");
            }

            // Deactivate match and conversation
            match.IsActive = false;
            match.UnmatchedAt = DateTime.UtcNow;
            match.UnmatchedById = userId;

            if (match.Conversation != null)
            {
                match.Conversation.IsActive = false;
            }

            await _context.SaveChangesAsync();

            // Track analytics
            await _analyticsService.TrackEventAsync(userId, "match_unmatched", new Dictionary<string, object>
            {
                {"match_id", matchId}
            });

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unmatching for user {UserId}, match {MatchId}", userId, matchId);
            return ServiceResult<bool>.Failure("An error occurred while unmatching");
        }
    }

    // Additional helper methods...

    private async Task<UserPreferences> GetUserPreferencesAsync(string userId)
    {
        var preferences = await _context.MatchingPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        return new UserPreferences
        {
            MinAge = preferences?.MinAge ?? 18,
            MaxAge = preferences?.MaxAge ?? 99,
            MaxDistance = preferences?.MaxDistance ?? 50,
            InterestedIn = preferences?.InterestedIn ?? "Everyone"
        };
    }

    private async Task<List<string>> GetExcludedUserIdsAsync(string userId)
    {
        var likedUserIds = await _context.Likes
            .Where(l => l.LikerId == userId)
            .Select(l => l.LikedId)
            .ToListAsync();

        var passedUserIds = await _context.Passes
            .Where(p => p.PasserId == userId)
            .Select(p => p.PassedId)
            .ToListAsync();

        var blockedUserIds = await _context.Blocks
            .Where(b => b.BlockerId == userId || b.BlockedId == userId)
            .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
            .ToListAsync();

        return likedUserIds.Union(passedUserIds).Union(blockedUserIds).Distinct().ToList();
    }

    private async Task<double> CalculateCompatibilityScoreAsync(ApplicationUser user1, ApplicationUser user2)
    {
        // Use AI service for compatibility calculation
        return await _aiService.CalculateCompatibilityScoreAsync(user1.Id, user2.Id);
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in kilometers

        var lat1Rad = Math.PI * lat1 / 180;
        var lat2Rad = Math.PI * lat2 / 180;
        var deltaLat = Math.PI * (lat2 - lat1) / 180;
        var deltaLon = Math.PI * (lon2 - lon1) / 180;

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    // Implement remaining interface methods...
    public async Task<ServiceResult<MatchingPreferencesResponse>> GetPreferencesAsync(string userId)
    {
        // Implementation here
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> UpdatePreferencesAsync(string userId, UpdatePreferencesRequest request)
    {
        // Implementation here
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> ActivateBoostAsync(string userId)
    {
        // Implementation here
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> BlockUserAsync(string userId, string targetUserId, string reason)
    {
        // Implementation here
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> ReportUserAsync(string userId, string targetUserId, string reason, string details)
    {
        // Implementation here
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<List<UserProfileResponse>>> GetLikedMeAsync(string userId)
    {
        // Implementation here
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<CompatibilityResponse>> GetCompatibilityScoreAsync(string userId, string targetUserId)
    {
        // Implementation here
        throw new NotImplementedException();
    }
}

// Helper classes
public class UserPreferences
{
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MaxDistance { get; set; }
    public string? InterestedIn { get; set; }
}