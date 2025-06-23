using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneTime.API.Models.DTOs;
using OneTime.API.Services;
using System.Security.Claims;

namespace OneTime.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamificationService;
    private readonly ILogger<GamificationController> _logger;

    public GamificationController(
        IGamificationService gamificationService,
        ILogger<GamificationController> logger)
    {
        _gamificationService = gamificationService;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetGamificationProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetGamificationProfileAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<GamificationProfileResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Gamification profile retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<GamificationProfileResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] string type = "weekly", [FromQuery] int limit = 50)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetLeaderboardAsync(type, limit);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<LeaderboardResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Leaderboard retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<LeaderboardResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("achievements")]
    public async Task<IActionResult> GetAchievements()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetUserAchievementsAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<AchievementResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Achievements retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<AchievementResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("badges")]
    public async Task<IActionResult> GetBadges()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetUserBadgesAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<BadgeResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Badges retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<BadgeResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("available-badges")]
    public async Task<IActionResult> GetAvailableBadges()
    {
        var result = await _gamificationService.GetAvailableBadgesAsync();
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<BadgeResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Available badges retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<BadgeResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("streaks")]
    public async Task<IActionResult> GetStreaks()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetUserStreaksAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<StreakResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Streaks retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<StreakResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("daily-check-in")]
    public async Task<IActionResult> DailyCheckIn()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.ProcessDailyCheckInAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<DailyCheckInResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Daily check-in completed successfully"
            });
        }

        return BadRequest(new ApiResponse<DailyCheckInResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("claim-reward")]
    public async Task<IActionResult> ClaimReward([FromBody] ClaimRewardRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.ClaimRewardAsync(userId, request.RewardId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<RewardResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Reward claimed successfully"
            });
        }

        return BadRequest(new ApiResponse<RewardResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("challenges")]
    public async Task<IActionResult> GetActiveChallenges()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetActiveChallengesAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<ChallengeResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Active challenges retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<ChallengeResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("challenges/{challengeId}/participate")]
    public async Task<IActionResult> ParticipateInChallenge(string challengeId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.ParticipateInChallengeAsync(userId, challengeId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Successfully joined challenge"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("level-progress")]
    public async Task<IActionResult> GetLevelProgress()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetLevelProgressAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<LevelProgressResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Level progress retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<LevelProgressResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("rewards/available")]
    public async Task<IActionResult> GetAvailableRewards()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetAvailableRewardsAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<RewardResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Available rewards retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<RewardResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("experience-history")]
    public async Task<IActionResult> GetExperienceHistory([FromQuery] int days = 30)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetExperienceHistoryAsync(userId, days);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<ExperienceHistoryResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Experience history retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<ExperienceHistoryResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("share-achievement")]
    public async Task<IActionResult> ShareAchievement([FromBody] ShareAchievementRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.ShareAchievementAsync(userId, request.AchievementId, request.Platform);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<ShareResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Achievement shared successfully"
            });
        }

        return BadRequest(new ApiResponse<ShareResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetGamificationDashboard()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _gamificationService.GetGamificationDashboardAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<GamificationDashboardResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Gamification dashboard retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<GamificationDashboardResponse>
        {
            Success = false,
            Message = result.Message
        });
    }
}