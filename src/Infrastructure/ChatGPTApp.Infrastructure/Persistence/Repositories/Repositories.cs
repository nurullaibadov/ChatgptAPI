using ChatGPTApp.Application.Interfaces.Repositories;
using ChatGPTApp.Domain.Entities;
using ChatGPTApp.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ChatGPTApp.Infrastructure.Persistence.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);

    public async Task<User?> GetByResetTokenAsync(string token, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.PasswordResetToken == token, cancellationToken);
}

public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
{
    public ConversationRepository(AppDbContext context) : base(context) { }

    public async Task<Conversation?> GetWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(c => c.Messages.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(c => c.UserId == userId)
            .Include(c => c.Messages.Where(m => !m.IsDeleted))
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(c => c.UserId == userId, cancellationToken);
}

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
}
