namespace ChatGPTApp.Application.DTOs.Chat;

public class CreateConversationDto
{
    public string? Title { get; set; }
    public string InitialMessage { get; set; } = null!;
}

public class SendMessageDto
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = null!;
    public string? Model { get; set; } = "gpt-3.5-turbo";
}

public class ConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
}

public class MessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int? TokensUsed { get; set; }
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChatCompletionRequestDto
{
    public string Model { get; set; } = "gpt-3.5-turbo";
    public string Message { get; set; } = null!;
    public Guid? ConversationId { get; set; }
}

public class ChatCompletionResponseDto
{
    public string Reply { get; set; } = null!;
    public int TokensUsed { get; set; }
    public string Model { get; set; } = null!;
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
