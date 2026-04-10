using ChatGPTApp.Application.DTOs;
using ChatGPTApp.Application.DTOs.Chat;
using ChatGPTApp.Application.Interfaces.Repositories;
using ChatGPTApp.Application.Interfaces.Services;
using ChatGPTApp.Domain.Entities;
using ChatGPTApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ChatGPTApp.Application.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IUnitOfWork unitOfWork, IOpenAIService openAIService, ILogger<ChatService> logger)
    {
        _unitOfWork = unitOfWork;
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<ApiResponse<ChatCompletionResponseDto>> SendMessageAsync(Guid userId, ChatCompletionRequestDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            Conversation conversation;

            if (dto.ConversationId.HasValue)
            {
                var existing = await _unitOfWork.Conversations.GetWithMessagesAsync(dto.ConversationId.Value, cancellationToken);
                if (existing == null || existing.UserId != userId)
                    return ApiResponse<ChatCompletionResponseDto>.Fail("Conversation not found.");
                conversation = existing;
            }
            else
            {
                conversation = new Conversation
                {
                    UserId = userId,
                    Title = dto.Message.Length > 50 ? dto.Message[..50] + "..." : dto.Message
                };
                await _unitOfWork.Conversations.AddAsync(conversation, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Add user message
            var userMessage = new Message
            {
                ConversationId = conversation.Id,
                Content = dto.Message,
                Role = MessageRole.User
            };
            await _unitOfWork.Messages.AddAsync(userMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Build message history for OpenAI
            var history = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => (m.Role == MessageRole.User ? "user" : "assistant", m.Content))
                .ToList();
            history.Add(("user", dto.Message));

            // Call OpenAI
            var model = dto.Model ?? "gpt-3.5-turbo";
            var (reply, tokensUsed) = await _openAIService.GetChatCompletionAsync(model, history, cancellationToken);

            // Save assistant message
            var assistantMessage = new Message
            {
                ConversationId = conversation.Id,
                Content = reply,
                Role = MessageRole.Assistant,
                TokensUsed = tokensUsed,
                Model = model
            };
            await _unitOfWork.Messages.AddAsync(assistantMessage, cancellationToken);

            conversation.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Conversations.Update(conversation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<ChatCompletionResponseDto>.Ok(new ChatCompletionResponseDto
            {
                Reply = reply,
                TokensUsed = tokensUsed,
                Model = model,
                ConversationId = conversation.Id,
                MessageId = assistantMessage.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message for user {UserId}", userId);
            return ApiResponse<ChatCompletionResponseDto>.Fail("Failed to process message.");
        }
    }

    public async Task<ApiResponse<ConversationDto>> GetConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conv = await _unitOfWork.Conversations.GetWithMessagesAsync(conversationId, cancellationToken);
        if (conv == null || conv.UserId != userId)
            return ApiResponse<ConversationDto>.Fail("Conversation not found.");

        return ApiResponse<ConversationDto>.Ok(MapToDto(conv));
    }

    public async Task<ApiResponse<PagedResult<ConversationDto>>> GetConversationsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var conversations = await _unitOfWork.Conversations.GetByUserIdAsync(userId, page, pageSize, cancellationToken);
        var totalCount = await _unitOfWork.Conversations.GetCountByUserIdAsync(userId, cancellationToken);

        return ApiResponse<PagedResult<ConversationDto>>.Ok(new PagedResult<ConversationDto>
        {
            Items = conversations.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse> DeleteConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conv = await _unitOfWork.Conversations.GetByIdAsync(conversationId, cancellationToken);
        if (conv == null || conv.UserId != userId)
            return ApiResponse.Fail("Conversation not found.");

        _unitOfWork.Conversations.SoftDelete(conv);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Conversation deleted.");
    }

    public async Task<ApiResponse<ConversationDto>> CreateConversationAsync(Guid userId, CreateConversationDto dto, CancellationToken cancellationToken = default)
    {
        var chatRequest = new ChatCompletionRequestDto
        {
            Message = dto.InitialMessage,
            Model = "gpt-3.5-turbo"
        };

        var result = await SendMessageAsync(userId, chatRequest, cancellationToken);
        if (!result.Success)
            return ApiResponse<ConversationDto>.Fail(result.Errors);

        if (!string.IsNullOrEmpty(dto.Title))
        {
            var conv = await _unitOfWork.Conversations.GetByIdAsync(result.Data!.ConversationId, cancellationToken);
            if (conv != null)
            {
                conv.Title = dto.Title;
                _unitOfWork.Conversations.Update(conv);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var conversation = await _unitOfWork.Conversations.GetWithMessagesAsync(result.Data!.ConversationId, cancellationToken);
        return ApiResponse<ConversationDto>.Ok(MapToDto(conversation!));
    }

    private static ConversationDto MapToDto(Conversation conv) => new()
    {
        Id = conv.Id,
        Title = conv.Title,
        CreatedAt = conv.CreatedAt,
        UpdatedAt = conv.UpdatedAt,
        MessageCount = conv.Messages.Count,
        Messages = conv.Messages.OrderBy(m => m.CreatedAt).Select(m => new MessageDto
        {
            Id = m.Id,
            Content = m.Content,
            Role = m.Role.ToString(),
            TokensUsed = m.TokensUsed,
            Model = m.Model,
            CreatedAt = m.CreatedAt
        }).ToList()
    };
}
