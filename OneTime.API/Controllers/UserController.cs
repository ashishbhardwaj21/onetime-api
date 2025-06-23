using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneTime.API.Models.DTOs;
using OneTime.API.Services;
using System.Security.Claims;

namespace OneTime.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        IBlobStorageService blobStorageService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.GetUserProfileAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<UserProfileResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Profile retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<UserProfileResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.UpdateUserProfileAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<UserProfileResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Profile updated successfully"
            });
        }

        return BadRequest(new ApiResponse<UserProfileResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("photos")]
    public async Task<IActionResult> UploadPhoto([FromForm] UploadPhotoRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (request.Photo == null || request.Photo.Length == 0)
        {
            return BadRequest(new ApiResponse<PhotoResponse>
            {
                Success = false,
                Message = "No photo provided"
            });
        }

        var result = await _userService.UploadPhotoAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<PhotoResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Photo uploaded successfully"
            });
        }

        return BadRequest(new ApiResponse<PhotoResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpDelete("photos/{photoId}")]
    public async Task<IActionResult> DeletePhoto(string photoId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.DeletePhotoAsync(userId, photoId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Photo deleted successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPut("photos/{photoId}/order")]
    public async Task<IActionResult> UpdatePhotoOrder(string photoId, [FromBody] UpdatePhotoOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.UpdatePhotoOrderAsync(userId, photoId, request.Order);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Photo order updated successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPut("photos/{photoId}/main")]
    public async Task<IActionResult> SetMainPhoto(string photoId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.SetMainPhotoAsync(userId, photoId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Main photo updated successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPut("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.UpdateLocationAsync(userId, request.Latitude, request.Longitude, request.City, request.Country);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Location updated successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("verification/phone")]
    public async Task<IActionResult> RequestPhoneVerification([FromBody] PhoneVerificationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.RequestPhoneVerificationAsync(userId, request.PhoneNumber);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Verification code sent successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("verification/phone/verify")]
    public async Task<IActionResult> VerifyPhoneNumber([FromBody] VerifyPhoneRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.VerifyPhoneNumberAsync(userId, request.Code);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Phone number verified successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("verification/photo")]
    public async Task<IActionResult> SubmitPhotoVerification([FromForm] PhotoVerificationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (request.VerificationPhoto == null || request.VerificationPhoto.Length == 0)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "No verification photo provided"
            });
        }

        var result = await _userService.SubmitPhotoVerificationAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Photo verification submitted successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("interests")]
    public async Task<IActionResult> GetAvailableInterests()
    {
        var result = await _userService.GetAvailableInterestsAsync();
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<InterestResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Interests retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<InterestResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPut("interests")]
    public async Task<IActionResult> UpdateInterests([FromBody] UpdateInterestsRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.UpdateUserInterestsAsync(userId, request.InterestIds);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Interests updated successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.GetUserSettingsAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<UserSettingsResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Settings retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<UserSettingsResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.UpdateUserSettingsAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Settings updated successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("premium/subscribe")]
    public async Task<IActionResult> SubscribePremium([FromBody] SubscribePremiumRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.SubscribeToPremiumAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<SubscriptionResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Premium subscription activated successfully"
            });
        }

        return BadRequest(new ApiResponse<SubscriptionResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpDelete("premium/cancel")]
    public async Task<IActionResult> CancelPremium()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.CancelPremiumSubscriptionAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Premium subscription cancelled successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.DeleteAccountAsync(userId, request.Reason, request.Feedback);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Account deleted successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetUserActivity([FromQuery] int days = 30)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.GetUserActivityAsync(userId, days);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<UserActivityResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "User activity retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<UserActivityResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetUserStatistics()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.GetUserStatisticsAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<UserStatisticsResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "User statistics retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<UserStatisticsResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.SubmitFeedbackAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Feedback submitted successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }
}