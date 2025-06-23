using System.ComponentModel.DataAnnotations;

namespace OneTime.API.Models.DTOs;

// Request DTOs
public class SignUpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public string Gender { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public bool AcceptedTerms { get; set; }
    public bool AcceptedPrivacyPolicy { get; set; }
}

public class SignInRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}

public class ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class Enable2FARequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
}

public class Verify2FARequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
}

public class SocialAuthRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty; // google, facebook, apple

    [Required]
    public string AccessToken { get; set; } = string.Empty;

    public string? IdToken { get; set; } // For Apple Sign In
}

// Response DTOs
public class AuthResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserProfileResponse? User { get; set; }
    public bool RequiresEmailVerification { get; set; }
    public bool Requires2FA { get; set; }
    public string? Message { get; set; }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class TwoFactorSetupResponse
{
    public string QrCodeImageUrl { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
    public List<string> RecoveryCodes { get; set; } = new();
}

public class FileUploadResponse
{
    public string? FileName { get; set; }
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
}