using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneTime.API.Models.DTOs;
using OneTime.API.Services;
using System.Security.Claims;

namespace OneTime.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MatchingController : ControllerBase
{
    private readonly IMatchingService _matchingService;
    private readonly ILogger<MatchingController> _logger;

    public MatchingController(
        IMatchingService matchingService,
        ILogger<MatchingController> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    [HttpGet("discover")]
    public async Task<IActionResult> DiscoverProfiles([FromQuery] int count = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.DiscoverProfilesAsync(userId, count);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<UserProfileResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Profiles discovered successfully"
            });
        }

        return BadRequest(new ApiResponse<List<UserProfileResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("like")]
    public async Task<IActionResult> LikeProfile([FromBody] LikeProfileRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.LikeProfileAsync(userId, request.TargetUserId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<MatchResponse>
            {
                Success = true,
                Data = result.Data,
                Message = result.Data?.IsMatch == true ? "It's a match!" : "Profile liked successfully"
            });
        }

        return BadRequest(new ApiResponse<MatchResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("pass")]
    public async Task<IActionResult> PassProfile([FromBody] PassProfileRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.PassProfileAsync(userId, request.TargetUserId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Profile passed successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("super-like")]
    public async Task<IActionResult> SuperLikeProfile([FromBody] SuperLikeProfileRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.SuperLikeProfileAsync(userId, request.TargetUserId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<MatchResponse>
            {
                Success = true,
                Data = result.Data,
                Message = result.Data?.IsMatch == true ? "Super match created!" : "Super like sent successfully"
            });
        }

        return BadRequest(new ApiResponse<MatchResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.GetMatchesAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<MatchResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Matches retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<MatchResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpDelete("matches/{matchId}")]
    public async Task<IActionResult> Unmatch(string matchId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.UnmatchAsync(userId, matchId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Unmatched successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.GetPreferencesAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<MatchingPreferencesResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Preferences retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<MatchingPreferencesResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.UpdatePreferencesAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Preferences updated successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("boost")]
    public async Task<IActionResult> ActivateBoost()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.ActivateBoostAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Boost activated successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("block")]
    public async Task<IActionResult> BlockUser([FromBody] BlockUserRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.BlockUserAsync(userId, request.TargetUserId, request.Reason);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "User blocked successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("report")]
    public async Task<IActionResult> ReportUser([FromBody] ReportUserRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.ReportUserAsync(userId, request.TargetUserId, request.Reason, request.Details);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "User reported successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("liked-me")]
    public async Task<IActionResult> GetLikedMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.GetLikedMeAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<UserProfileResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Liked me profiles retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<UserProfileResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("compatibility/{targetUserId}")]
    public async Task<IActionResult> GetCompatibilityScore(string targetUserId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _matchingService.GetCompatibilityScoreAsync(userId, targetUserId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<CompatibilityResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Compatibility score calculated successfully"
            });
        }

        return BadRequest(new ApiResponse<CompatibilityResponse>
        {
            Success = false,
            Message = result.Message
        });
    }
}