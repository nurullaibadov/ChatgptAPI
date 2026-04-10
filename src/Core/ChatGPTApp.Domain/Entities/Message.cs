using ChatGPTApp.Domain.Common;
using ChatGPTApp.Domain.Enums;

namespace ChatGPTApp.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public string Content { get; set; } = null!;
    public MessageRole Role { get; set; }
    public int? TokensUsed { get; set; }
    public string? Model { get; set; }
}
