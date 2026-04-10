using ChatGPTApp.Application.DTOs;
using ChatGPTApp.Application.DTOs.Auth;
using ChatGPTApp.Application.Interfaces.Repositories;
using ChatGPTApp.Application.Interfaces.Services;
using ChatGPTApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ChatGPTApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, IEmailService emailService, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (dto.Password != dto.ConfirmPassword)
                return ApiResponse<AuthResponseDto>.Fail("Passwords do not match.");

            var existingUser = await _unitOfWork.Users.GetByEmailAsync(dto.Email, cancellationToken);
            if (existingUser != null)
                return ApiResponse<AuthResponseDto>.Fail("Email already in use.");

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RefreshToken = _jwtService.GenerateRefreshToken(),
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName, cancellationToken);

            var accessToken = _jwtService.GenerateAccessToken(user);
            return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken!,
                AccessTokenExpiry = DateTime.UtcNow.AddHours(1),
                User = MapToUserDto(user)
            }, "Registration successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", dto.Email);
            return ApiResponse<AuthResponseDto>.Fail("An error occurred during registration.");
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email.ToLowerInvariant(), cancellationToken);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");

            if (!user.IsActive)
                return ApiResponse<AuthResponseDto>.Fail("Account is disabled. Contact support.");

            user.RefreshToken = _jwtService.GenerateRefreshToken();
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                AccessToken = _jwtService.GenerateAccessToken(user),
                RefreshToken = user.RefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddHours(1),
                User = MapToUserDto(user)
            }, "Login successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", dto.Email);
            return ApiResponse<AuthResponseDto>.Fail("An error occurred during login.");
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _jwtService.GetUserIdFromToken(dto.AccessToken);
        if (userId == null)
            return ApiResponse<AuthResponseDto>.Fail("Invalid token.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null || user.RefreshToken != dto.RefreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
            return ApiResponse<AuthResponseDto>.Fail("Invalid or expired refresh token.");

        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            RefreshToken = user.RefreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddHours(1),
            User = MapToUserDto(user)
        });
    }

    public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email.ToLowerInvariant(), cancellationToken);
        if (user == null)
            return ApiResponse.Ok("If this email exists, a reset link has been sent.");

        user.PasswordResetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(2);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.PasswordResetToken, cancellationToken);
        return ApiResponse.Ok("If this email exists, a reset link has been sent.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return ApiResponse.Fail("Passwords do not match.");

        var user = await _unitOfWork.Users.GetByResetTokenAsync(dto.Token, cancellationToken);
        if (user == null || user.Email.ToLower() != dto.Email.ToLower())
            return ApiResponse.Fail("Invalid token.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return ApiResponse.Fail("Token expired. Please request a new reset link.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Password reset successful.");
    }

    public async Task<ApiResponse> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return ApiResponse.Fail("Passwords do not match.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return ApiResponse.Fail("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return ApiResponse.Fail("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Password changed successfully.");
    }

    public async Task<ApiResponse> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null) return ApiResponse.Fail("User not found.");

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Logged out successfully.");
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
