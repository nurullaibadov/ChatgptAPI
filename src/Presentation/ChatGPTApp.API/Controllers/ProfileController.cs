using ChatGPTApp.Application.DTOs.Auth;
using ChatGPTApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatGPTApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public ProfileController(IUserService userService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    /// <summary>Get my profile</summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(_currentUser.UserId!.Value, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Update my profile</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateAsync(_currentUser.UserId!.Value, dto, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
