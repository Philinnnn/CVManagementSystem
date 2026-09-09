namespace CVManagementSystem.Services.Discussions;

using Dtos;
using Common;

public interface IDiscussionService
{
    Task<List<DiscussionMessageDto>> GetMessagesAsync(int positionId);
    Task<OperationResult<DiscussionMessageDto>> PostMessageAsync(int positionId, int userId, string text);
}