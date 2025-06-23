using Microsoft.EntityFrameworkCore;
using OneTime.API.Data;
using OneTime.API.Models;
using OneTime.API.Models.DTOs;
using OneTime.API.Models.Entities;
using SixLabors.ImageSharp;

namespace OneTime.API.Services;

public interface IUserService
{
    Task<ServiceResult<UserProfileResponse>> GetUserProfileAsync(string userId);
    Task<ServiceResult<UserProfileResponse>> UpdateUserProfileAsync(string userId, UpdateProfileRequest request);
    Task<ServiceResult<PhotoResponse>> UploadPhotoAsync(string userId, UploadPhotoRequest request);
    Task<ServiceResult<bool>> DeletePhotoAsync(string userId, string photoId);
    Task<ServiceResult<bool>> UpdatePhotoOrderAsync(string userId, string photoId, int order);
    Task<ServiceResult<bool>> SetMainPhotoAsync(string userId, string photoId);
    Task<ServiceResult<bool>> UpdateLocationAsync(string userId, double latitude, double longitude, string? city, string? country);
    Task<ServiceResult<bool>> RequestPhoneVerificationAsync(string userId, string phoneNumber);
    Task<ServiceResult<bool>> VerifyPhoneNumberAsync(string userId, string code);
    Task<ServiceResult<bool>> SubmitPhotoVerificationAsync(string userId, PhotoVerificationRequest request);
    Task<ServiceResult<List<InterestResponse>>> GetAvailableInterestsAsync();
    Task<ServiceResult<bool>> UpdateUserInterestsAsync(string userId, List<string> interestIds);
    Task<ServiceResult<UserSettingsResponse>> GetUserSettingsAsync(string userId);
    Task<ServiceResult<bool>> UpdateUserSettingsAsync(string userId, UpdateSettingsRequest request);
    Task<ServiceResult<SubscriptionResponse>> SubscribeToPremiumAsync(string userId, SubscribePremiumRequest request);
    Task<ServiceResult<bool>> CancelPremiumSubscriptionAsync(string userId);
    Task<ServiceResult<bool>> DeleteAccountAsync(string userId, string reason, string? feedback);
    Task<ServiceResult<UserActivityResponse>> GetUserActivityAsync(string userId, int days);
    Task<ServiceResult<UserStatisticsResponse>> GetUserStatisticsAsync(string userId);
    Task<ServiceResult<bool>> SubmitFeedbackAsync(string userId, SubmitFeedbackRequest request);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly INotificationService _notificationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context,
        IBlobStorageService blobStorageService,
        INotificationService notificationService,
        IAnalyticsService analyticsService,
        ILogger<UserService> logger)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _notificationService = notificationService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<ServiceResult<UserProfileResponse>> GetUserProfileAsync(string userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.Photos.OrderBy(p => p.Order))
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.UserInterests)
                .ThenInclude(ui => ui.Interest)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ServiceResult<UserProfileResponse>.Failure("User not found");
            }

            var response = new UserProfileResponse
            {
                Id = user.Id,
                Name = $"{user.FirstName} {user.LastName}",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Age = DateTime.Today.Year - user.DateOfBirth.Year,
                Bio = user.Bio,
                Occupation = user.Occupation,
                Education = user.Education,
                Height = user.Height,
                Drinking = user.Drinking,
                Smoking = user.Smoking,
                Children = user.Children,
                Religion = user.Religion,
                PoliticalViews = user.PoliticalViews,
                Photos = user.UserProfile?.Photos?.Select(p => new PhotoResponse
                {
                    Id = p.Id,
                    Url = p.Url,
                    ThumbnailUrl = p.ThumbnailUrl,
                    Order = p.Order,
                    IsMain = p.IsMain,
                    CreatedAt = p.CreatedAt
                }).ToList() ?? new List<PhotoResponse>(),
                Interests = user.UserProfile?.UserInterests?.Select(ui => ui.Interest.Name).ToList() ?? new List<string>(),
                IsVerified = user.IsVerified,
                LastActive = user.LastActive,
                IsPremium = user.IsPremium,
                IsOnline = DateTime.UtcNow - user.LastActive < TimeSpan.FromMinutes(5)
            };

            return ServiceResult<UserProfileResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile for {UserId}", userId);
            return ServiceResult<UserProfileResponse>.Failure("An error occurred while getting the profile");
        }
    }

    public async Task<ServiceResult<UserProfileResponse>> UpdateUserProfileAsync(string userId, UpdateProfileRequest request)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ServiceResult<UserProfileResponse>.Failure("User not found");
            }

            // Update user fields
            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;
            
            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;
            
            if (!string.IsNullOrEmpty(request.Bio))
                user.Bio = request.Bio;
            
            if (!string.IsNullOrEmpty(request.Occupation))
                user.Occupation = request.Occupation;
            
            if (!string.IsNullOrEmpty(request.Education))
                user.Education = request.Education;
            
            if (request.Height.HasValue)
                user.Height = request.Height.Value;
            
            if (!string.IsNullOrEmpty(request.Drinking))
                user.Drinking = request.Drinking;
            
            if (!string.IsNullOrEmpty(request.Smoking))
                user.Smoking = request.Smoking;
            
            if (!string.IsNullOrEmpty(request.Children))
                user.Children = request.Children;
            
            if (!string.IsNullOrEmpty(request.Religion))
                user.Religion = request.Religion;
            
            if (!string.IsNullOrEmpty(request.PoliticalViews))
                user.PoliticalViews = request.PoliticalViews;

            user.UpdatedAt = DateTime.UtcNow;

            // Create or update user profile
            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserProfiles.Add(user.UserProfile);
            }

            user.UserProfile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Calculate completion percentage
            await UpdateProfileCompletionAsync(userId);

            // Track analytics
            await _analyticsService.TrackEventAsync(userId, "profile_updated", new Dictionary<string, object>
            {
                {"fields_updated", GetUpdatedFields(request).Count},
                {"timestamp", DateTime.UtcNow}
            });

            // Get updated profile
            return await GetUserProfileAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for {UserId}", userId);
            return ServiceResult<UserProfileResponse>.Failure("An error occurred while updating the profile");
        }
    }

    public async Task<ServiceResult<PhotoResponse>> UploadPhotoAsync(string userId, UploadPhotoRequest request)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.Photos)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ServiceResult<PhotoResponse>.Failure("User not found");
            }

            // Check photo limit
            var maxPhotos = user.IsPremium ? 20 : 5;
            var currentPhotoCount = user.UserProfile?.Photos?.Count ?? 0;
            
            if (currentPhotoCount >= maxPhotos)
            {
                return ServiceResult<PhotoResponse>.Failure($"Maximum of {maxPhotos} photos allowed");
            }

            // Upload image to blob storage
            var uploadResult = await _blobStorageService.UploadImageAsync(
                request.Photo, 
                "profile-photos", 
                userId, 
                generateThumbnail: true);

            if (!uploadResult.Success)
            {
                return ServiceResult<PhotoResponse>.Failure(uploadResult.Message);
            }

            // If this is the first photo or main is requested, set as main
            var isMain = request.IsMain || currentPhotoCount == 0;

            // If setting as main, unset other main photos
            if (isMain && user.UserProfile?.Photos != null)
            {
                foreach (var existingPhoto in user.UserProfile.Photos.Where(p => p.IsMain))
                {
                    existingPhoto.IsMain = false;
                }
            }

            // Create photo record
            var photo = new Photo
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Url = uploadResult.Data.Url,
                ThumbnailUrl = uploadResult.Data.ThumbnailUrl,
                Order = request.Order > 0 ? request.Order : currentPhotoCount + 1,
                IsMain = isMain,
                IsApproved = true, // Auto-approve for now, can add moderation later
                CreatedAt = DateTime.UtcNow
            };

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            // Update profile completion
            await UpdateProfileCompletionAsync(userId);

            var response = new PhotoResponse
            {
                Id = photo.Id,
                Url = photo.Url,
                ThumbnailUrl = photo.ThumbnailUrl,
                Order = photo.Order,
                IsMain = photo.IsMain,
                CreatedAt = photo.CreatedAt
            };

            // Track analytics
            await _analyticsService.TrackEventAsync(userId, "photo_uploaded", new Dictionary<string, object>
            {
                {"photo_count", currentPhotoCount + 1},
                {"is_main", isMain}
            });

            return ServiceResult<PhotoResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for user {UserId}", userId);
            return ServiceResult<PhotoResponse>.Failure("An error occurred while uploading the photo");
        }
    }

    public async Task<ServiceResult<bool>> DeletePhotoAsync(string userId, string photoId)
    {
        try
        {
            var photo = await _context.Photos
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId);

            if (photo == null)
            {
                return ServiceResult<bool>.Failure("Photo not found");
            }

            // Delete from blob storage
            var containerName = "profile-photos";
            var fileName = Path.GetFileName(new Uri(photo.Url).LocalPath);
            await _blobStorageService.DeleteFileAsync(containerName, $"{userId}/{fileName}");

            // Delete thumbnail if exists
            if (!string.IsNullOrEmpty(photo.ThumbnailUrl))
            {
                var thumbnailFileName = Path.GetFileName(new Uri(photo.ThumbnailUrl).LocalPath);
                await _blobStorageService.DeleteFileAsync(containerName, $"{userId}/thumbnails/{thumbnailFileName}");
            }

            // Delete from database
            _context.Photos.Remove(photo);

            // If this was the main photo, set another photo as main
            if (photo.IsMain)
            {
                var otherPhoto = await _context.Photos
                    .Where(p => p.UserId == userId && p.Id != photoId)
                    .OrderBy(p => p.Order)
                    .FirstOrDefaultAsync();

                if (otherPhoto != null)
                {
                    otherPhoto.IsMain = true;
                }
            }

            await _context.SaveChangesAsync();

            // Update profile completion
            await UpdateProfileCompletionAsync(userId);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo {PhotoId} for user {UserId}", photoId, userId);
            return ServiceResult<bool>.Failure("An error occurred while deleting the photo");
        }
    }

    public async Task<ServiceResult<bool>> UpdatePhotoOrderAsync(string userId, string photoId, int order)
    {
        try
        {
            var photo = await _context.Photos
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId);

            if (photo == null)
            {
                return ServiceResult<bool>.Failure("Photo not found");
            }

            photo.Order = order;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating photo order for {PhotoId}", photoId);
            return ServiceResult<bool>.Failure("An error occurred while updating photo order");
        }
    }

    public async Task<ServiceResult<bool>> SetMainPhotoAsync(string userId, string photoId)
    {
        try
        {
            var photos = await _context.Photos
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var targetPhoto = photos.FirstOrDefault(p => p.Id == photoId);
            if (targetPhoto == null)
            {
                return ServiceResult<bool>.Failure("Photo not found");
            }

            // Unset all main photos
            foreach (var photo in photos)
            {
                photo.IsMain = photo.Id == photoId;
            }

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting main photo {PhotoId} for user {UserId}", photoId, userId);
            return ServiceResult<bool>.Failure("An error occurred while setting main photo");
        }
    }

    public async Task<ServiceResult<bool>> UpdateLocationAsync(string userId, double latitude, double longitude, string? city, string? country)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ServiceResult<bool>.Failure("User not found");
            }

            user.Latitude = latitude;
            user.Longitude = longitude;
            user.City = city;
            user.Country = country;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for user {UserId}", userId);
            return ServiceResult<bool>.Failure("An error occurred while updating location");
        }
    }

    public async Task<ServiceResult<List<InterestResponse>>> GetAvailableInterestsAsync()
    {
        try
        {
            var interests = await _context.Interests
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .ToListAsync();

            var response = interests.Select(i => new InterestResponse
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                Icon = i.Icon
            }).ToList();

            return ServiceResult<List<InterestResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available interests");
            return ServiceResult<List<InterestResponse>>.Failure("An error occurred while getting interests");
        }
    }

    public async Task<ServiceResult<bool>> UpdateUserInterestsAsync(string userId, List<string> interestIds)
    {
        try
        {
            // Remove existing interests
            var existingInterests = await _context.UserInterests
                .Where(ui => ui.UserId == userId)
                .ToListAsync();

            _context.UserInterests.RemoveRange(existingInterests);

            // Add new interests (limit to 5)
            var limitedInterestIds = interestIds.Take(5).ToList();
            var newInterests = limitedInterestIds.Select(interestId => new UserInterest
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                InterestId = interestId,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.UserInterests.AddRange(newInterests);
            await _context.SaveChangesAsync();

            // Update profile completion
            await UpdateProfileCompletionAsync(userId);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating interests for user {UserId}", userId);
            return ServiceResult<bool>.Failure("An error occurred while updating interests");
        }
    }

    // Implement remaining interface methods with NotImplementedException for now
    public async Task<ServiceResult<bool>> RequestPhoneVerificationAsync(string userId, string phoneNumber)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> VerifyPhoneNumberAsync(string userId, string code)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> SubmitPhotoVerificationAsync(string userId, PhotoVerificationRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<UserSettingsResponse>> GetUserSettingsAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> UpdateUserSettingsAsync(string userId, UpdateSettingsRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<SubscriptionResponse>> SubscribeToPremiumAsync(string userId, SubscribePremiumRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> CancelPremiumSubscriptionAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> DeleteAccountAsync(string userId, string reason, string? feedback)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<UserActivityResponse>> GetUserActivityAsync(string userId, int days)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<UserStatisticsResponse>> GetUserStatisticsAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> SubmitFeedbackAsync(string userId, SubmitFeedbackRequest request)
    {
        throw new NotImplementedException();
    }

    // Helper methods
    private async Task UpdateProfileCompletionAsync(string userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.Photos)
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.UserInterests)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.UserProfile == null) return;

            var completionScore = 0;
            var totalFields = 10;

            // Basic info (2 points)
            if (!string.IsNullOrEmpty(user.Bio)) completionScore++;
            if (!string.IsNullOrEmpty(user.Occupation)) completionScore++;

            // Photos (3 points)
            var photoCount = user.UserProfile.Photos?.Count ?? 0;
            if (photoCount >= 1) completionScore++;
            if (photoCount >= 3) completionScore++;
            if (photoCount >= 5) completionScore++;

            // Interests (2 points)
            var interestCount = user.UserProfile.UserInterests?.Count ?? 0;
            if (interestCount >= 3) completionScore++;
            if (interestCount >= 5) completionScore++;

            // Additional info (3 points)
            if (!string.IsNullOrEmpty(user.Education)) completionScore++;
            if (user.Height.HasValue) completionScore++;
            if (!string.IsNullOrEmpty(user.Drinking)) completionScore++;

            user.UserProfile.CompletionPercentage = (int)((double)completionScore / totalFields * 100);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile completion for {UserId}", userId);
        }
    }

    private static List<string> GetUpdatedFields(UpdateProfileRequest request)
    {
        var fields = new List<string>();
        
        if (!string.IsNullOrEmpty(request.FirstName)) fields.Add("firstName");
        if (!string.IsNullOrEmpty(request.LastName)) fields.Add("lastName");
        if (!string.IsNullOrEmpty(request.Bio)) fields.Add("bio");
        if (!string.IsNullOrEmpty(request.Occupation)) fields.Add("occupation");
        if (!string.IsNullOrEmpty(request.Education)) fields.Add("education");
        if (request.Height.HasValue) fields.Add("height");
        if (!string.IsNullOrEmpty(request.Drinking)) fields.Add("drinking");
        if (!string.IsNullOrEmpty(request.Smoking)) fields.Add("smoking");
        if (!string.IsNullOrEmpty(request.Children)) fields.Add("children");
        if (!string.IsNullOrEmpty(request.Religion)) fields.Add("religion");
        if (!string.IsNullOrEmpty(request.PoliticalViews)) fields.Add("politicalViews");

        return fields;
    }
}