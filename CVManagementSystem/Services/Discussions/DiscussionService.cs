namespace CVManagementSystem.Services.Discussions;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Models.Discussions;
using Common;

public class DiscussionService(AppDbContext db) : IDiscussionService
{
    public async Task<List<DiscussionMessageDto>> GetMessagesAsync(int positionId)
    {
        var discussion = await db.Discussions
            .Include(d => d.Messages).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(d => d.PositionId == positionId);

        return discussion is null
            ? []
            : discussion.Messages.OrderBy(m => m.SendAt).Select(ToDto).ToList();
    }

    public async Task<OperationResult<DiscussionMessageDto>> PostMessageAsync(int positionId, int userId, string text)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return OperationResult<DiscussionMessageDto>.Fail("Message cannot be empty");

        if (!await db.Positions.AnyAsync(p => p.Id == positionId))
            return OperationResult<DiscussionMessageDto>.Fail("Position not found");

        var discussion = await db.Discussions.FirstOrDefaultAsync(d => d.PositionId == positionId);
        if (discussion is null)
        {
            discussion = new Discussion { PositionId = positionId };
            db.Discussions.Add(discussion);
        }

        var message = new DiscussionMessage
        {
            Discussion = discussion,
            UserId = userId,
            MessageText = text
        };

        db.DiscussionMessages.Add(message);
        await db.SaveChangesAsync();

        var saved = await db.DiscussionMessages.Include(m => m.User).FirstAsync(m => m.Id == message.Id);
        return OperationResult<DiscussionMessageDto>.Ok(ToDto(saved));
    }

    private static DiscussionMessageDto ToDto(DiscussionMessage message) => new()
    {
        Id = message.Id,
        UserId = message.UserId,
        UserFullname = message.User.Fullname,
        MessageText = message.MessageText,
        SendAt = message.SendAt
    };
}