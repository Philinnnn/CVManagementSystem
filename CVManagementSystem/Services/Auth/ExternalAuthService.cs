namespace CVManagementSystem.Services.Auth;

using Data;
using Microsoft.EntityFrameworkCore;
using Models.Candidates;
using Models.Identity;
using Common;

public class ExternalAuthService(AppDbContext db) : IExternalAuthService
{
    public async Task<OperationResult<User>> LoginAsync(string provider, string providerKey, string? email, string? name)
    {
        var existingLogin = await db.ExternalLogins
            .Include(el => el.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(el => el.Provider == provider && el.ProviderKey == providerKey);

        if (existingLogin is not null)
        {
            if (existingLogin.User.IsBlocked)
                return OperationResult<User>.Fail("This account has been blocked");

            return OperationResult<User>.Ok(existingLogin.User);
        }
        
        if (!string.IsNullOrWhiteSpace(email) && await db.Users.AnyAsync(u => u.Email == email))
        {
            return OperationResult<User>.Fail(
                "An account with this email already exists. Log in with your password and link this provider from Account Settings.");
        }

        var candidateRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Candidate")
            ?? throw new InvalidOperationException("Candidate role not seeded");

        var user = new User
        {
            Login = await GenerateUniqueLoginAsync(email, name),
            Email = email ?? $"{provider.ToLowerInvariant()}_{providerKey}@no-email.local",
            Fullname = name ?? "New User",
            Passhash = Guid.NewGuid().ToString("N")
        };
        user.UserRoles.Add(new UserRole { Role = candidateRole });
        user.Candidate = new Candidate();

        var builtInAttributeIds = await db.Attributes.Where(a => a.IsBuiltIn).Select(a => a.Id).ToListAsync();
        foreach (var attributeId in builtInAttributeIds)
            user.Candidate.AttributeValues.Add(new CandidateAttributeValue { AttributeId = attributeId });

        db.Users.Add(user);
        db.ExternalLogins.Add(new ExternalLogin { User = user, Provider = provider, ProviderKey = providerKey });
        await db.SaveChangesAsync();

        await db.Entry(user).Collection(u => u.UserRoles).Query().Include(ur => ur.Role).LoadAsync();
        return OperationResult<User>.Ok(user);
    }

    public async Task<OperationResult> LinkAsync(int userId, string provider, string providerKey)
    {
        var conflictingLink = await db.ExternalLogins
            .FirstOrDefaultAsync(el => el.Provider == provider && el.ProviderKey == providerKey);

        if (conflictingLink is not null)
        {
            return conflictingLink.UserId == userId
                ? OperationResult.Ok()
                : OperationResult.Fail("This provider account is already linked to a different user.");
        }

        db.ExternalLogins.Add(new ExternalLogin { UserId = userId, Provider = provider, ProviderKey = providerKey });
        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    private async Task<string> GenerateUniqueLoginAsync(string? email, string? name)
    {
        var basis = new string((email?.Split('@')[0] ?? name ?? "user")
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

        if (string.IsNullOrEmpty(basis))
            basis = "user";

        var candidate = basis;
        var suffix = 1;
        while (await db.Users.AnyAsync(u => u.Login == candidate))
        {
            candidate = $"{basis}{suffix}";
            suffix++;
        }

        return candidate;
    }
}