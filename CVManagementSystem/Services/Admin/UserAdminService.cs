namespace CVManagementSystem.Services.Admin;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Models.Identity;
using Common;

public class UserAdminService(AppDbContext db) : IUserAdminService
{
    public async Task<List<UserAdminDto>> GetAllAsync()
    {
        var users = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Candidate)
            .Include(u => u.CreatedPositions)
            .OrderBy(u => u.Login)
            .ToListAsync();

        return users.Select(u => new UserAdminDto
        {
            Id = u.Id,
            Login = u.Login,
            Email = u.Email,
            Fullname = u.Fullname,
            IsBlocked = u.IsBlocked,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
            CandidateId = u.Candidate?.Id,
            HasCreatedPositions = u.CreatedPositions.Count > 0
        }).ToList();
    }

    public async Task<List<string>> GetAllRoleNamesAsync() =>
        await db.Roles.OrderBy(r => r.Name).Select(r => r.Name).ToListAsync();

    public async Task<OperationResult> SetBlockedAsync(IEnumerable<int> userIds, bool blocked)
    {
        var ids = userIds.ToList();
        var users = await db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();

        foreach (var user in users)
            user.IsBlocked = blocked;

        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> AssignRoleAsync(IEnumerable<int> userIds, string roleName)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role is null)
            return OperationResult.Fail("Role not found");

        var ids = userIds.ToList();
        var existing = await db.UserRoles
            .Where(ur => ids.Contains(ur.UserId) && ur.RoleId == role.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        foreach (var userId in ids.Except(existing))
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });

        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> RemoveRoleAsync(IEnumerable<int> userIds, string roleName)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role is null)
            return OperationResult.Fail("Role not found");

        var ids = userIds.ToList();
        var toRemove = await db.UserRoles
            .Where(ur => ids.Contains(ur.UserId) && ur.RoleId == role.Id)
            .ToListAsync();

        db.UserRoles.RemoveRange(toRemove);
        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.ToList();
        var users = await db.Users
            .Include(u => u.CreatedPositions)
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();

        var blockers = users.Where(u => u.CreatedPositions.Count > 0).Select(u => u.Login).ToList();
        if (blockers.Count > 0)
            return OperationResult.Fail(
                $"Cannot delete users who created positions (deleting would cascade-delete those positions): {string.Join(", ", blockers)}. Block them instead.");

        db.Users.RemoveRange(users);
        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }
}