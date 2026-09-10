namespace CVManagementSystem.Services.Admin;

using Dtos;
using Common;

public interface IUserAdminService
{
    Task<List<UserAdminDto>> GetAllAsync();
    Task<List<string>> GetAllRoleNamesAsync();
    Task<OperationResult> SetBlockedAsync(IEnumerable<int> userIds, bool blocked);
    Task<OperationResult> AssignRoleAsync(IEnumerable<int> userIds, string roleName);
    Task<OperationResult> RemoveRoleAsync(IEnumerable<int> userIds, string roleName);
    Task<OperationResult> DeleteAsync(IEnumerable<int> userIds);
}