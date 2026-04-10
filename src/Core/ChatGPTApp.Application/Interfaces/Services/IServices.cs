using ChatGPTApp.Application.DTOs;
using ChatGPTApp.Application.DTOs.Auth;
using ChatGPTApp.Application.DTOs.Chat;

namespace ChatGPTApp.Application.Interfaces.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<ApiResponse<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<UserDto>>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse> ChangeRoleAsync(Guid id, string role, CancellationToken cancellationToken = default);
}

public interface IChatService
{
    Task<ApiResponse<ChatCompletionResponseDto>> SendMessageAsync(Guid userId, ChatCompletionRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<ConversationDto>> GetConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<ConversationDto>>> GetConversationsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ConversationDto>> CreateConversationAsync(Guid userId, CreateConversationDto dto, CancellationToken cancellationToken = default);
}

public interface IJwtService
{
    string GenerateAccessToken(Domain.Entities.User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
    bool ValidateToken(string token);
}

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string toEmail, string firstName, CancellationToken cancellationToken = default);
}

public interface IOpenAIService
{
    Task<(string Reply, int TokensUsed)> GetChatCompletionAsync(
        string model,
        List<(string Role, string Content)> messages,
        CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
