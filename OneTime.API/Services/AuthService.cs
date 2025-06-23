using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OneTime.API.Models;
using OneTime.API.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using QRCoder;
using OtpNet;
using OneTime.API.Data;
using Microsoft.EntityFrameworkCore;

namespace OneTime.API.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> SignUpAsync(SignUpRequest request);
    Task<ServiceResult<AuthResponse>> SignInAsync(SignInRequest request);
    Task<ServiceResult<AuthResponse>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResult<bool>> VerifyEmailAsync(VerifyEmailRequest request);
    Task<ServiceResult<bool>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ServiceResult<TwoFactorSetupResponse>> Setup2FAAsync(string userId);
    Task<ServiceResult<AuthResponse>> Verify2FAAsync(Verify2FARequest request);
    Task<ServiceResult<bool>> Disable2FAAsync(string userId, string password);
    Task SignOutAsync(string userId);
    Task<ServiceResult<bool>> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<ServiceResult<UserResponse>> GetCurrentUserAsync(string userId);
    Task<ServiceResult<bool>> DeleteAccountAsync(string userId, DeleteAccountRequest request);
    Task<ServiceResult<bool>> ResendVerificationEmailAsync(string email);
    Task<bool> IsEmailAvailableAsync(string email);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ISMSService _smsService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        IEmailService emailService,
        ISMSService smsService,
        IAnalyticsService analyticsService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _smsService = smsService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResponse>> SignUpAsync(SignUpRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return ServiceResult<AuthResponse>.Failure("An account with this email already exists");
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                InterestedIn = request.InterestedIn,
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ServiceResult<AuthResponse>.Failure("Registration failed", errors);
            }

            // Add user to default role
            await _userManager.AddToRoleAsync(user, "User");

            // Generate email confirmation token
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            
            // Send verification email
            await _emailService.SendVerificationEmailAsync(user.Email, user.Id, emailToken);

            // Track analytics
            await _analyticsService.TrackEventAsync(user.Id, "user_signup", new Dictionary<string, object>
            {
                {"method", "email"},
                {"timestamp", DateTime.UtcNow}
            });

            // Generate tokens for immediate sign-in (email still needs verification)
            var authResponse = await GenerateAuthResponseAsync(user);

            _logger.LogInformation("User {UserId} registered successfully", user.Id);

            return ServiceResult<AuthResponse>.Success(authResponse, "Registration successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return ServiceResult<AuthResponse>.Failure("An error occurred during registration");
        }
    }

    public async Task<ServiceResult<AuthResponse>> SignInAsync(SignInRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return ServiceResult<AuthResponse>.Failure("Invalid email or password");
            }

            if (user.IsBlocked)
            {
                return ServiceResult<AuthResponse>.Failure("Your account has been blocked. Please contact support.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            
            if (result.IsLockedOut)
            {
                return ServiceResult<AuthResponse>.Failure("Account locked due to multiple failed attempts. Please try again later.");
            }

            if (!result.Succeeded)
            {
                return ServiceResult<AuthResponse>.Failure("Invalid email or password");
            }

            // Check if 2FA is enabled
            if (user.IsTwoFactorEnabled)
            {
                // Return partial response indicating 2FA is required
                return ServiceResult<AuthResponse>.Success(new AuthResponse
                {
                    RequiresTwoFactor = true,
                    UserId = user.Id
                }, "Two-factor authentication required");
            }

            // Update last active
            user.LastActive = DateTime.UtcNow;
            user.IsOnline = true;
            await _userManager.UpdateAsync(user);

            // Track analytics
            await _analyticsService.TrackEventAsync(user.Id, "user_signin", new Dictionary<string, object>
            {
                {"method", "email"},
                {"timestamp", DateTime.UtcNow}
            });

            var authResponse = await GenerateAuthResponseAsync(user);

            _logger.LogInformation("User {UserId} signed in successfully", user.Id);

            return ServiceResult<AuthResponse>.Success(authResponse, "Sign in successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user sign in");
            return ServiceResult<AuthResponse>.Failure("An error occurred during sign in");
        }
    }

    public async Task<ServiceResult<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return ServiceResult<AuthResponse>.Failure("Invalid or expired refresh token");
            }

            var authResponse = await GenerateAuthResponseAsync(user);
            
            return ServiceResult<AuthResponse>.Success(authResponse, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return ServiceResult<AuthResponse>.Failure("An error occurred during token refresh");
        }
    }

    public async Task<ServiceResult<bool>> VerifyEmailAsync(VerifyEmailRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return ServiceResult<bool>.Failure("User not found");
            }

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            
            if (result.Succeeded)
            {
                user.IsVerified = true;
                await _userManager.UpdateAsync(user);

                // Track analytics
                await _analyticsService.TrackEventAsync(user.Id, "email_verified", new Dictionary<string, object>
                {
                    {"timestamp", DateTime.UtcNow}
                });

                return ServiceResult<bool>.Success(true, "Email verified successfully");
            }

            return ServiceResult<bool>.Failure("Email verification failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            return ServiceResult<bool>.Failure("An error occurred during email verification");
        }
    }

    public async Task<ServiceResult<bool>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal whether user exists
                return ServiceResult<bool>.Success(true, "Reset email sent if account exists");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Id, token);

            return ServiceResult<bool>.Success(true, "Reset email sent if account exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request");
            return ServiceResult<bool>.Failure("An error occurred while processing your request");
        }
    }

    public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return ServiceResult<bool>.Failure("Invalid reset token");
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            
            if (result.Succeeded)
            {
                // Track analytics
                await _analyticsService.TrackEventAsync(user.Id, "password_reset", new Dictionary<string, object>
                {
                    {"timestamp", DateTime.UtcNow}
                });

                return ServiceResult<bool>.Success(true, "Password reset successfully");
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            return ServiceResult<bool>.Failure("Password reset failed", errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return ServiceResult<bool>.Failure("An error occurred during password reset");
        }
    }

    public async Task<ServiceResult<TwoFactorSetupResponse>> Setup2FAAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<TwoFactorSetupResponse>.Failure("User not found");
            }

            // Generate secret key
            var key = KeyGeneration.GenerateRandomKey(20);
            var secret = Base32Encoding.ToString(key);
            
            user.TwoFactorSecret = secret;
            await _userManager.UpdateAsync(user);

            // Generate QR code
            var appName = _configuration["App:Name"] ?? "OneTime";
            var qrCodeText = $"otpauth://totp/{appName}:{user.Email}?secret={secret}&issuer={appName}";
            
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrCodeText, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            // Generate backup codes
            var backupCodes = GenerateBackupCodes();
            
            var response = new TwoFactorSetupResponse
            {
                Secret = secret,
                QrCode = Convert.ToBase64String(qrCodeBytes),
                BackupCodes = backupCodes
            };

            return ServiceResult<TwoFactorSetupResponse>.Success(response, "2FA setup initiated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA setup");
            return ServiceResult<TwoFactorSetupResponse>.Failure("An error occurred during 2FA setup");
        }
    }

    public async Task<ServiceResult<AuthResponse>> Verify2FAAsync(Verify2FARequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return ServiceResult<AuthResponse>.Failure("User not found");
            }

            bool isValidCode = false;

            if (!string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret));
                isValidCode = totp.VerifyTotp(request.Code, out long timeStepMatched, window: TimeSpan.FromSeconds(30));
            }

            if (!isValidCode)
            {
                return ServiceResult<AuthResponse>.Failure("Invalid verification code");
            }

            // Enable 2FA if this is the first verification
            if (!user.IsTwoFactorEnabled)
            {
                user.IsTwoFactorEnabled = true;
                await _userManager.UpdateAsync(user);
            }

            // Update last active
            user.LastActive = DateTime.UtcNow;
            user.IsOnline = true;
            await _userManager.UpdateAsync(user);

            var authResponse = await GenerateAuthResponseAsync(user);

            return ServiceResult<AuthResponse>.Success(authResponse, "2FA verification successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA verification");
            return ServiceResult<AuthResponse>.Failure("An error occurred during 2FA verification");
        }
    }

    public async Task<ServiceResult<bool>> Disable2FAAsync(string userId, string password)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<bool>.Failure("User not found");
            }

            // Verify password
            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                return ServiceResult<bool>.Failure("Invalid password");
            }

            user.IsTwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            await _userManager.UpdateAsync(user);

            return ServiceResult<bool>.Success(true, "2FA disabled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA disable");
            return ServiceResult<bool>.Failure("An error occurred while disabling 2FA");
        }
    }

    public async Task SignOutAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsOnline = false;
                user.LastOnline = DateTime.UtcNow;
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.UtcNow;
                
                await _userManager.UpdateAsync(user);

                // Track analytics
                await _analyticsService.TrackEventAsync(userId, "user_signout", new Dictionary<string, object>
                {
                    {"timestamp", DateTime.UtcNow}
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign out for user {UserId}", userId);
        }
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<bool>.Failure("User not found");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            
            if (result.Succeeded)
            {
                return ServiceResult<bool>.Success(true, "Password changed successfully");
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            return ServiceResult<bool>.Failure("Password change failed", errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return ServiceResult<bool>.Failure("An error occurred during password change");
        }
    }

    public async Task<ServiceResult<UserResponse>> GetCurrentUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<UserResponse>.Failure("User not found");
            }

            var userResponse = new UserResponse
            {
                Id = user.Id,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                IsPhoneConfirmed = user.PhoneNumberConfirmed,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                IsVerified = user.IsVerified,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                InterestedIn = user.InterestedIn,
                Bio = user.Bio,
                Occupation = user.Occupation,
                Education = user.Education,
                City = user.City,
                State = user.State,
                Country = user.Country,
                SubscriptionType = user.SubscriptionType,
                HasPremium = user.IsPremiumActive,
                CreatedAt = user.CreatedAt,
                LastActive = user.LastActive
            };

            return ServiceResult<UserResponse>.Success(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return ServiceResult<UserResponse>.Failure("An error occurred while getting user information");
        }
    }

    public async Task<ServiceResult<bool>> DeleteAccountAsync(string userId, DeleteAccountRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<bool>.Failure("User not found");
            }

            // Verify password
            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return ServiceResult<bool>.Failure("Invalid password");
            }

            // Soft delete - mark as deleted instead of actually deleting
            user.DeletedAt = DateTime.UtcNow;
            user.DeleteReason = request.Reason;
            user.IsActive = false;
            
            await _userManager.UpdateAsync(user);

            // Send confirmation email
            await _emailService.SendAccountDeletionConfirmationAsync(user.Email!);

            return ServiceResult<bool>.Success(true, "Account deletion initiated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account deletion");
            return ServiceResult<bool>.Failure("An error occurred during account deletion");
        }
    }

    public async Task<ServiceResult<bool>> ResendVerificationEmailAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.EmailConfirmed)
            {
                // Don't reveal whether user exists or is already verified
                return ServiceResult<bool>.Success(true, "Verification email sent if account exists");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailService.SendVerificationEmailAsync(user.Email!, user.Id, token);

            return ServiceResult<bool>.Success(true, "Verification email sent if account exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email");
            return ServiceResult<bool>.Failure("An error occurred while sending verification email");
        }
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user == null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email availability");
            return false;
        }
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var (accessToken, refreshToken) = await GenerateTokensAsync(user);
        
        // Update refresh token in database
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // 7 days
        await _userManager.UpdateAsync(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600, // 1 hour
            TokenType = "Bearer",
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email!,
                IsEmailConfirmed = user.EmailConfirmed,
                IsVerified = user.IsVerified,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                HasPremium = user.IsPremiumActive
            }
        };
    }

    private async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"]!;
        var jwtIssuer = _configuration["Jwt:Issuer"]!;
        var jwtAudience = _configuration["Jwt:Audience"]!;

        var authClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        var userRoles = await _userManager.GetRolesAsync(user);
        authClaims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var accessToken = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            expires: DateTime.UtcNow.AddHours(1),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        var refreshToken = GenerateRefreshToken();

        return (new JwtSecurityTokenHandler().WriteToken(accessToken), refreshToken);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static List<string> GenerateBackupCodes()
    {
        var codes = new List<string>();
        var random = new Random();
        
        for (int i = 0; i < 10; i++)
        {
            var code = random.Next(100000, 999999).ToString();
            codes.Add(code);
        }
        
        return codes;
    }
}