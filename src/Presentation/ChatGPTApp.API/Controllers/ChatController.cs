using ChatGPTApp.Application.DTOs.Chat;
using ChatGPTApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatGPTApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ICurrentUserService _currentUser;

    public ChatController(IChatService chatService, ICurrentUserService currentUser)
    {
        _chatService = chatService;
        _currentUser = currentUser;
    }

    /// <summary>Send a chat message and get AI reply</summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] ChatCompletionRequestDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        var result = await _chatService.SendMessageAsync(userId, dto, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Create a new conversation with an initial message</summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        var result = await _chatService.CreateConversationAsync(userId, dto, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get all conversations for current user (paginated)</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId!.Value;
        var result = await _chatService.GetConversationsAsync(userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a specific conversation with messages</summary>
    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        var result = await _chatService.GetConversationAsync(userId, id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Delete a conversation</summary>
    [HttpDelete("conversations/{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        var result = await _chatService.DeleteConversationAsync(userId, id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
