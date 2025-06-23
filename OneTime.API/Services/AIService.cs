using Azure.AI.OpenAI;
using Azure.AI.TextAnalytics;
using Microsoft.EntityFrameworkCore;
using OneTime.API.Data;
using OneTime.API.Models;
using OneTime.API.Models.DTOs;
using System.Text.Json;

namespace OneTime.API.Services;

public interface IAIService
{
    Task<double> CalculateCompatibilityScoreAsync(string userId1, string userId2);
    Task<ServiceResult<List<UserProfileResponse>>> GetAISuggestedMatchesAsync(string userId, int count = 10);
    Task<ServiceResult<bool>> ModerateContentAsync(string content, string contentType);
    Task<ServiceResult<string>> GenerateConversationStarterAsync(string userId, string targetUserId);
    Task<ServiceResult<List<string>>> ExtractInterestsFromBioAsync(string bio);
    Task<ServiceResult<string>> GenerateProfileSuggestionAsync(string userId);
    Task<ServiceResult<bool>> DetectInappropriateImageAsync(byte[] imageData);
    Task<ServiceResult<CompatibilityAnalysis>> GetDetailedCompatibilityAsync(string userId1, string userId2);
}

public class AIService : IAIService
{
    private readonly ApplicationDbContext _context;
    private readonly OpenAIClient? _openAIClient;
    private readonly TextAnalyticsClient? _textAnalyticsClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIService> _logger;
    private readonly bool _aiEnabled;

    public AIService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AIService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _aiEnabled = _configuration.GetValue<bool>("Features:EnableAIMatching", false);

        try
        {
            // Initialize Azure OpenAI client
            var openAIEndpoint = _configuration["AI:Azure:OpenAI:Endpoint"];
            var openAIKey = _configuration["AI:Azure:OpenAI:ApiKey"];
            
            if (!string.IsNullOrEmpty(openAIEndpoint) && !string.IsNullOrEmpty(openAIKey))
            {
                _openAIClient = new OpenAIClient(new Uri(openAIEndpoint), new Azure.AzureKeyCredential(openAIKey));
            }

            // Initialize Text Analytics client
            var textAnalyticsEndpoint = _configuration["Azure:CognitiveServices:TextAnalytics:Endpoint"];
            var textAnalyticsKey = _configuration["Azure:CognitiveServices:TextAnalytics:SubscriptionKey"];
            
            if (!string.IsNullOrEmpty(textAnalyticsEndpoint) && !string.IsNullOrEmpty(textAnalyticsKey))
            {
                _textAnalyticsClient = new TextAnalyticsClient(new Uri(textAnalyticsEndpoint), new Azure.AzureKeyCredential(textAnalyticsKey));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize AI services");
        }
    }

    public async Task<double> CalculateCompatibilityScoreAsync(string userId1, string userId2)
    {
        try
        {
            if (!_aiEnabled)
            {
                return CalculateBasicCompatibilityScore(userId1, userId2);
            }

            var user1 = await GetUserWithDetailsAsync(userId1);
            var user2 = await GetUserWithDetailsAsync(userId2);

            if (user1 == null || user2 == null)
            {
                return 0.0;
            }

            var compatibilityFactors = new List<CompatibilityFactor>();

            // Age compatibility (10%)
            var ageScore = CalculateAgeCompatibility(user1, user2);
            compatibilityFactors.Add(new CompatibilityFactor
            {
                Name = "Age",
                Score = ageScore,
                Description = $"Age difference: {Math.Abs(user1.Age - user2.Age)} years"
            });

            // Interest compatibility (25%)
            var interestScore = CalculateInterestCompatibility(user1, user2);
            compatibilityFactors.Add(new CompatibilityFactor
            {
                Name = "Interests",
                Score = interestScore,
                Description = "Shared interests and hobbies"
            });

            // Lifestyle compatibility (20%)
            var lifestyleScore = CalculateLifestyleCompatibility(user1, user2);
            compatibilityFactors.Add(new CompatibilityFactor
            {
                Name = "Lifestyle",
                Score = lifestyleScore,
                Description = "Drinking, smoking, and life choices"
            });

            // Values compatibility (15%)
            var valuesScore = CalculateValuesCompatibility(user1, user2);
            compatibilityFactors.Add(new CompatibilityFactor
            {
                Name = "Values",
                Score = valuesScore,
                Description = "Religion and political views"
            });

            // Education/Career compatibility (10%)
            var careerScore = CalculateCareerCompatibility(user1, user2);
            compatibilityFactors.Add(new CompatibilityFactor
            {
                Name = "Career",
                Score = careerScore,
                Description = "Education and professional background"
            });

            // Bio compatibility using AI (20%)
            var bioScore = await CalculateBioCompatibilityAsync(user1.Bio, user2.Bio);
            compatibilityFactors.Add(new CompatibilityFactor
            {
                Name = "Personality",
                Score = bioScore,
                Description = "Personality analysis from bio"
            });

            // Calculate weighted average
            var totalScore = (ageScore * 0.10) +
                           (interestScore * 0.25) +
                           (lifestyleScore * 0.20) +
                           (valuesScore * 0.15) +
                           (careerScore * 0.10) +
                           (bioScore * 0.20);

            return Math.Round(totalScore, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating compatibility between {UserId1} and {UserId2}", userId1, userId2);
            return CalculateBasicCompatibilityScore(userId1, userId2);
        }
    }

    public async Task<ServiceResult<List<UserProfileResponse>>> GetAISuggestedMatchesAsync(string userId, int count = 10)
    {
        try
        {
            if (!_aiEnabled)
            {
                return ServiceResult<List<UserProfileResponse>>.Failure("AI matching is not enabled");
            }

            var user = await GetUserWithDetailsAsync(userId);
            if (user == null)
            {
                return ServiceResult<List<UserProfileResponse>>.Failure("User not found");
            }

            // Get user's matching preferences
            var preferences = await _context.MatchingPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // Get excluded user IDs (already liked, passed, blocked)
            var excludedUserIds = await GetExcludedUserIdsAsync(userId);

            // Get potential matches based on basic criteria
            var potentialMatches = await _context.Users
                .Where(u => u.Id != userId && 
                           !excludedUserIds.Contains(u.Id) &&
                           u.IsActive && 
                           !u.IsBlocked &&
                           u.ShowMeOnDiscovery)
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.Photos)
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.UserInterests)
                .ThenInclude(ui => ui.Interest)
                .Take(count * 5) // Get more than needed for AI ranking
                .ToListAsync();

            // Calculate AI compatibility scores for each potential match
            var scoredMatches = new List<(ApplicationUser user, double score)>();
            
            foreach (var match in potentialMatches)
            {
                var score = await CalculateCompatibilityScoreAsync(userId, match.Id);
                scoredMatches.Add((match, score));
            }

            // Sort by compatibility score and take top matches
            var topMatches = scoredMatches
                .OrderByDescending(x => x.score)
                .Take(count)
                .Select(x => x.user)
                .ToList();

            // Convert to response DTOs
            var response = topMatches.Select(match => new UserProfileResponse
            {
                Id = match.Id,
                Name = $"{match.FirstName} {match.LastName}",
                Age = match.Age,
                Bio = match.Bio,
                Photos = match.UserProfile?.Photos?.OrderBy(p => p.Order)
                    .Select(p => new PhotoResponse
                    {
                        Id = p.Id,
                        Url = p.Url,
                        ThumbnailUrl = p.ThumbnailUrl,
                        Order = p.Order,
                        IsMain = p.IsMain
                    }).ToList() ?? new List<PhotoResponse>(),
                Interests = match.UserProfile?.UserInterests?.Select(ui => ui.Interest.Name).ToList() ?? new List<string>(),
                IsVerified = match.IsVerified,
                LastActive = match.LastActive,
                Occupation = match.Occupation,
                Education = match.Education
            }).ToList();

            return ServiceResult<List<UserProfileResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI suggested matches for user {UserId}", userId);
            return ServiceResult<List<UserProfileResponse>>.Failure("An error occurred while getting AI suggestions");
        }
    }

    public async Task<ServiceResult<bool>> ModerateContentAsync(string content, string contentType)
    {
        try
        {
            if (_textAnalyticsClient == null)
            {
                _logger.LogWarning("Text Analytics client not configured, skipping content moderation");
                return ServiceResult<bool>.Success(true);
            }

            // Analyze content for toxic, profane, or inappropriate material
            var documents = new List<string> { content };
            
            // Check for personally identifiable information
            var piiResults = await _textAnalyticsClient.RecognizePiiEntitiesAsync(documents);
            
            foreach (var result in piiResults.Value)
            {
                if (result.HasError) continue;
                
                // Flag content if it contains sensitive PII
                var sensitivePii = result.Entities.Where(e => 
                    e.Category == Azure.AI.TextAnalytics.PiiEntityCategory.PhoneNumber ||
                    e.Category == Azure.AI.TextAnalytics.PiiEntityCategory.Email ||
                    e.Category == Azure.AI.TextAnalytics.PiiEntityCategory.Address);
                
                if (sensitivePii.Any())
                {
                    _logger.LogWarning("Content blocked due to PII detection: {ContentType}", contentType);
                    return ServiceResult<bool>.Success(false);
                }
            }

            // In a real implementation, you'd also check for:
            // - Inappropriate language using custom models
            // - Spam detection
            // - Harassment detection
            // - Adult content detection

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moderating content of type {ContentType}", contentType);
            // Default to allowing content if moderation fails
            return ServiceResult<bool>.Success(true);
        }
    }

    public async Task<ServiceResult<string>> GenerateConversationStarterAsync(string userId, string targetUserId)
    {
        try
        {
            if (_openAIClient == null)
            {
                return ServiceResult<string>.Success("Hey! How's your day going?");
            }

            var user = await GetUserWithDetailsAsync(userId);
            var target = await GetUserWithDetailsAsync(targetUserId);

            if (user == null || target == null)
            {
                return ServiceResult<string>.Success("Hey! How's your day going?");
            }

            // Find common interests
            var commonInterests = user.Interests.Intersect(target.Interests).ToList();
            
            var prompt = $@"Generate a friendly, casual conversation starter for a dating app. 
User 1: {user.FirstName}, interests: {string.Join(", ", user.Interests)}
User 2: {target.FirstName}, interests: {string.Join(", ", target.Interests)}
Common interests: {string.Join(", ", commonInterests)}

The message should be:
- Friendly and casual
- Reference a common interest if available
- Be engaging and invite a response
- Be 1-2 sentences max
- Not be cheesy or overly forward

Generate just the message, no explanations.";

            var response = await _openAIClient.GetChatCompletionsAsync(
                new ChatCompletionsOptions()
                {
                    DeploymentName = _configuration["AI:Azure:OpenAI:DeploymentId"],
                    Messages = {
                        new ChatRequestSystemMessage("You are a helpful assistant that generates conversation starters for a dating app."),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = 100,
                    Temperature = 0.7f
                });

            var conversationStarter = response.Value.Choices[0].Message.Content.Trim();
            
            return ServiceResult<string>.Success(conversationStarter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating conversation starter");
            return ServiceResult<string>.Success("Hey! How's your day going?");
        }
    }

    // Helper methods
    private async Task<UserWithDetails?> GetUserWithDetailsAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.UserProfile)
            .ThenInclude(up => up.UserInterests)
            .ThenInclude(ui => ui.Interest)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new UserWithDetails
        {
            Id = user.Id,
            FirstName = user.FirstName,
            Age = DateTime.Today.Year - user.DateOfBirth.Year,
            Bio = user.Bio ?? string.Empty,
            Occupation = user.Occupation ?? string.Empty,
            Education = user.Education ?? string.Empty,
            Drinking = user.Drinking ?? string.Empty,
            Smoking = user.Smoking ?? string.Empty,
            Children = user.Children ?? string.Empty,
            Religion = user.Religion ?? string.Empty,
            PoliticalViews = user.PoliticalViews ?? string.Empty,
            Interests = user.UserProfile?.UserInterests?.Select(ui => ui.Interest.Name).ToList() ?? new List<string>()
        };
    }

    private static double CalculateBasicCompatibilityScore(string userId1, string userId2)
    {
        // Basic compatibility based on user IDs (for demo purposes)
        var hash1 = userId1.GetHashCode();
        var hash2 = userId2.GetHashCode();
        var combined = Math.Abs(hash1 ^ hash2);
        return (combined % 100) / 100.0;
    }

    private static double CalculateAgeCompatibility(UserWithDetails user1, UserWithDetails user2)
    {
        var ageDiff = Math.Abs(user1.Age - user2.Age);
        return ageDiff switch
        {
            <= 2 => 1.0,
            <= 5 => 0.8,
            <= 10 => 0.6,
            <= 15 => 0.4,
            _ => 0.2
        };
    }

    private static double CalculateInterestCompatibility(UserWithDetails user1, UserWithDetails user2)
    {
        if (!user1.Interests.Any() || !user2.Interests.Any())
            return 0.5;

        var commonInterests = user1.Interests.Intersect(user2.Interests).Count();
        var totalInterests = user1.Interests.Union(user2.Interests).Count();
        
        return (double)commonInterests / Math.Max(totalInterests, 1);
    }

    private static double CalculateLifestyleCompatibility(UserWithDetails user1, UserWithDetails user2)
    {
        var score = 0.0;
        var factors = 0;

        // Drinking compatibility
        if (!string.IsNullOrEmpty(user1.Drinking) && !string.IsNullOrEmpty(user2.Drinking))
        {
            score += user1.Drinking == user2.Drinking ? 1.0 : 0.5;
            factors++;
        }

        // Smoking compatibility
        if (!string.IsNullOrEmpty(user1.Smoking) && !string.IsNullOrEmpty(user2.Smoking))
        {
            score += user1.Smoking == user2.Smoking ? 1.0 : 0.3;
            factors++;
        }

        // Children compatibility
        if (!string.IsNullOrEmpty(user1.Children) && !string.IsNullOrEmpty(user2.Children))
        {
            score += user1.Children == user2.Children ? 1.0 : 0.4;
            factors++;
        }

        return factors > 0 ? score / factors : 0.5;
    }

    private static double CalculateValuesCompatibility(UserWithDetails user1, UserWithDetails user2)
    {
        var score = 0.0;
        var factors = 0;

        // Religion compatibility
        if (!string.IsNullOrEmpty(user1.Religion) && !string.IsNullOrEmpty(user2.Religion))
        {
            score += user1.Religion == user2.Religion ? 1.0 : 0.3;
            factors++;
        }

        // Political views compatibility
        if (!string.IsNullOrEmpty(user1.PoliticalViews) && !string.IsNullOrEmpty(user2.PoliticalViews))
        {
            score += user1.PoliticalViews == user2.PoliticalViews ? 1.0 : 0.2;
            factors++;
        }

        return factors > 0 ? score / factors : 0.5;
    }

    private static double CalculateCareerCompatibility(UserWithDetails user1, UserWithDetails user2)
    {
        var score = 0.0;
        var factors = 0;

        // Education level compatibility
        if (!string.IsNullOrEmpty(user1.Education) && !string.IsNullOrEmpty(user2.Education))
        {
            score += user1.Education == user2.Education ? 1.0 : 0.7;
            factors++;
        }

        // Career field compatibility (simplified)
        if (!string.IsNullOrEmpty(user1.Occupation) && !string.IsNullOrEmpty(user2.Occupation))
        {
            var sameField = user1.Occupation.Contains(user2.Occupation, StringComparison.OrdinalIgnoreCase) ||
                           user2.Occupation.Contains(user1.Occupation, StringComparison.OrdinalIgnoreCase);
            score += sameField ? 1.0 : 0.6;
            factors++;
        }

        return factors > 0 ? score / factors : 0.5;
    }

    private async Task<double> CalculateBioCompatibilityAsync(string bio1, string bio2)
    {
        try
        {
            if (string.IsNullOrEmpty(bio1) || string.IsNullOrEmpty(bio2) || _openAIClient == null)
            {
                return 0.5;
            }

            var prompt = $@"Analyze the compatibility between these two dating profiles based on their bios. 
Return only a number between 0.0 and 1.0 representing compatibility (1.0 = perfect match, 0.0 = no compatibility).

Bio 1: {bio1}
Bio 2: {bio2}

Consider:
- Personality traits
- Life goals and aspirations
- Communication style
- Humor compatibility
- Shared values from the text

Return only the decimal number, no explanations.";

            var response = await _openAIClient.GetChatCompletionsAsync(
                new ChatCompletionsOptions()
                {
                    DeploymentName = _configuration["AI:Azure:OpenAI:DeploymentId"],
                    Messages = {
                        new ChatRequestSystemMessage("You are an AI that analyzes dating profile compatibility. Return only decimal numbers between 0.0 and 1.0."),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = 10,
                    Temperature = 0.3f
                });

            var scoreText = response.Value.Choices[0].Message.Content.Trim();
            
            if (double.TryParse(scoreText, out var score))
            {
                return Math.Max(0.0, Math.Min(1.0, score));
            }

            return 0.5;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating bio compatibility");
            return 0.5;
        }
    }

    private async Task<List<string>> GetExcludedUserIdsAsync(string userId)
    {
        var likedIds = await _context.Likes.Where(l => l.LikerId == userId).Select(l => l.LikedId).ToListAsync();
        var passedIds = await _context.Passes.Where(p => p.PasserId == userId).Select(p => p.PassedId).ToListAsync();
        var blockedIds = await _context.Blocks.Where(b => b.BlockerId == userId || b.BlockedId == userId)
            .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId).ToListAsync();

        return likedIds.Union(passedIds).Union(blockedIds).Distinct().ToList();
    }

    // Implement remaining interface methods with NotImplementedException for now
    public async Task<ServiceResult<List<string>>> ExtractInterestsFromBioAsync(string bio)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<string>> GenerateProfileSuggestionAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> DetectInappropriateImageAsync(byte[] imageData)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<CompatibilityAnalysis>> GetDetailedCompatibilityAsync(string userId1, string userId2)
    {
        throw new NotImplementedException();
    }
}

// Helper classes
public class UserWithDetails
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;
    public string Education { get; set; } = string.Empty;
    public string Drinking { get; set; } = string.Empty;
    public string Smoking { get; set; } = string.Empty;
    public string Children { get; set; } = string.Empty;
    public string Religion { get; set; } = string.Empty;
    public string PoliticalViews { get; set; } = string.Empty;
    public List<string> Interests { get; set; } = new();
}

public class CompatibilityAnalysis
{
    public double OverallScore { get; set; }
    public List<CompatibilityFactor> Factors { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> PotentialChallenges { get; set; } = new();
}