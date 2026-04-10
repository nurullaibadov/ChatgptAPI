using ChatGPTApp.Application.DTOs;
using ChatGPTApp.Application.DTOs.Auth;
using ChatGPTApp.Application.DTOs.Chat;
using ChatGPTApp.Application.Interfaces.Repositories;
using ChatGPTApp.Application.Interfaces.Services;
using ChatGPTApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ChatGPTApp.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null) return ApiResponse<UserDto>.Fail("User not found.");

        return ApiResponse<UserDto>.Ok(MapToDto(user));
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        var filtered = users.Where(u => !u.IsDeleted).ToList();
        var totalCount = filtered.Count;
        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();

        return ApiResponse<PagedResult<UserDto>>.Ok(new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null) return ApiResponse<UserDto>.Fail("User not found.");

        var existingEmail = await _unitOfWork.Users.GetByEmailAsync(dto.Email.ToLowerInvariant(), cancellationToken);
        if (existingEmail != null && existingEmail.Id != id)
            return ApiResponse<UserDto>.Fail("Email already in use.");

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email.ToLowerInvariant();
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<UserDto>.Ok(MapToDto(user), "User updated successfully.");
    }

    public async Task<ApiResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null) return ApiResponse.Fail("User not found.");

        _unitOfWork.Users.SoftDelete(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("User deleted.");
    }

    public async Task<ApiResponse> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null) return ApiResponse.Fail("User not found.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok($"User {(user.IsActive ? "activated" : "deactivated")} successfully.");
    }

    public async Task<ApiResponse> ChangeRoleAsync(Guid id, string role, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null) return ApiResponse.Fail("User not found.");

        if (!Enum.TryParse<UserRole>(role, true, out var userRole))
            return ApiResponse.Fail("Invalid role.");

        user.Role = userRole;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok($"Role changed to {role}.");
    }

    private static UserDto MapToDto(Domain.Entities.User user) => new()
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
