using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneTime.API.Models.DTOs;
using OneTime.API.Services;
using System.Security.Claims;

namespace OneTime.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.SignUpAsync(request);
            
            if (result.Success)
            {
                _logger.LogInformation("User registered successfully: {Email}", request.Email);
                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Registration successful. Please verify your email.",
                    Data = result.Data
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message,
                Errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during registration"
            });
        }
    }

    /// <summary>
    /// Sign in with email and password
    /// </summary>
    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.SignInAsync(request);
            
            if (result.Success)
            {
                _logger.LogInformation("User signed in successfully: {Email}", request.Email);
                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Sign in successful",
                    Data = result.Data
                });
            }

            return Unauthorized(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user sign in");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during sign in"
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Data = result.Data
                });
            }

            return Unauthorized(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during token refresh"
            });
        }
    }

    /// <summary>
    /// Verify email address
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            var result = await _authService.VerifyEmailAsync(request);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Email verified successfully"
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during email verification"
            });
        }
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var result = await _authService.ForgotPasswordAsync(request);
            
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "If an account with that email exists, a password reset link has been sent."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while processing your request"
            });
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var result = await _authService.ResetPasswordAsync(request);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Password reset successfully"
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during password reset"
            });
        }
    }

    /// <summary>
    /// Setup two-factor authentication
    /// </summary>
    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<IActionResult> Setup2FA()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _authService.Setup2FAAsync(userId);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<TwoFactorSetupResponse>
                {
                    Success = true,
                    Message = "2FA setup initiated",
                    Data = result.Data
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA setup");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during 2FA setup"
            });
        }
    }

    /// <summary>
    /// Verify two-factor authentication code
    /// </summary>
    [HttpPost("2fa/verify")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request)
    {
        try
        {
            var result = await _authService.Verify2FAAsync(request);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "2FA verification successful",
                    Data = result.Data
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA verification");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during 2FA verification"
            });
        }
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _authService.Disable2FAAsync(userId, request.Password);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "2FA disabled successfully"
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA disable");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while disabling 2FA"
            });
        }
    }

    /// <summary>
    /// Sign out user
    /// </summary>
    [HttpPost("signout")]
    [Authorize]
    public async Task<IActionResult> SignOut()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _authService.SignOutAsync(userId);
            
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Signed out successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign out");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during sign out"
            });
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _authService.ChangePasswordAsync(userId, request);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Password changed successfully"
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during password change"
            });
        }
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _authService.GetCurrentUserAsync(userId);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<UserResponse>
                {
                    Success = true,
                    Data = result.Data
                });
            }

            return NotFound(new ApiResponse<string>
            {
                Success = false,
                Message = "User not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while getting user information"
            });
        }
    }

    /// <summary>
    /// Delete user account
    /// </summary>
    [HttpDelete("delete-account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _authService.DeleteAccountAsync(userId, request);
            
            if (result.Success)
            {
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Account deletion initiated. You will receive a confirmation email."
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account deletion");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during account deletion"
            });
        }
    }

    /// <summary>
    /// Resend email verification
    /// </summary>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        try
        {
            var result = await _authService.ResendVerificationEmailAsync(request.Email);
            
            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "If an account with that email exists, a verification email has been sent."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while sending verification email"
            });
        }
    }

    /// <summary>
    /// Check if email is available
    /// </summary>
    [HttpGet("check-email/{email}")]
    public async Task<IActionResult> CheckEmailAvailability(string email)
    {
        try
        {
            var isAvailable = await _authService.IsEmailAvailableAsync(email);
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = isAvailable,
                Message = isAvailable ? "Email is available" : "Email is already taken"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email availability");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while checking email availability"
            });
        }
    }

    /// <summary>
    /// Get password strength requirements
    /// </summary>
    [HttpGet("password-requirements")]
    public IActionResult GetPasswordRequirements()
    {
        return Ok(new ApiResponse<PasswordRequirements>
        {
            Success = true,
            Data = new PasswordRequirements
            {
                MinLength = 8,
                RequireDigit = true,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireNonAlphanumeric = true,
                AllowedNonAlphanumericCharacters = "!@#$%^&*()_+-=[]{}|;:,.<>?"
            }
        });
    }
}