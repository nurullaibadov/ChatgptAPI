using ChatGPTApp.Domain.Common;

namespace ChatGPTApp.Domain.Entities;

public class Conversation : BaseEntity
{
    public string Title { get; set; } = "New Conversation";
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
