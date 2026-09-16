namespace CVManagementSystem.Services.Auth;

using Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.Candidates;
using Models.Identity;

public class AuthService(AppDbContext db, IPasswordHasher<User> hasher) : IAuthService
{
    public async Task<(bool Success, string? Error, User? User)> RegisterAsync(
        string login, string email, string password, string fullname)
    {
        login = login.Trim();
        email = email.Trim();

        if (await db.Users.AnyAsync(u => u.Login == login))
            return (false, "User with such login already exists", null);

        if (await db.Users.AnyAsync(u => u.Email == email))
            return (false, "User with such email already exists", null);

        var candidateRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Candidate");
        if (candidateRole is null)
            return (false, "Candidate role not found — check seed data", null);

        var user = new User
        {
            Login = login,
            Email = email,
            Fullname = fullname
        };
        user.Passhash = hasher.HashPassword(user, password);
        user.UserRoles.Add(new UserRole { Role = candidateRole });
        user.Candidate = new Candidate();
        var builtInAttributeIds = await db.Attributes
            .Where(a => a.IsBuiltIn)
            .Select(a => a.Id)
            .ToListAsync();

        foreach (var attributeId in builtInAttributeIds)
        {
            user.Candidate.AttributeValues.Add(new CandidateAttributeValue { AttributeId = attributeId });
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (true, null, user);
    }

    public async Task<(bool Success, string? Error, User? User)> ValidateCredentialsAsync(
        string login, string password)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Login == login);

        if (user is null)
            return (false, "Invalid login or password", null);

        if (user.IsBlocked)
            return (false, "Account is blocked", null);

        var result = hasher.VerifyHashedPassword(user, user.Passhash, password);
        if (result == PasswordVerificationResult.Failed)
            return (false, "Invalid login or password", null);

        return (true, null, user);
    }
}