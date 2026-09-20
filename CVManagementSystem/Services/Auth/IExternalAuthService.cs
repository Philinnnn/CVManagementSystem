namespace CVManagementSystem.Services.Auth;

using Models.Identity;
using Common;

public interface IExternalAuthService
{
    Task<OperationResult<(User User, bool IsNewUser)>> LoginAsync(string provider, string providerKey, string? email, string? name);
    Task<OperationResult> LinkAsync(int userId, string provider, string providerKey);
    Task<OperationResult> UnlinkAsync(int userId, string provider);
}