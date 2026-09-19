namespace CVManagementSystem.Services.Auth;

using Models.Identity;

public interface IAuthService
{
    Task<(bool Success, string? Error, User? User)> RegisterAsync(string login, string email, string password, string fullname);
    Task<(bool Success, string? Error, User? User)> ValidateCredentialsAsync(string email, string password);
    Task<(bool Success, string? Error)> SetPasswordAsync(int userId, string newPassword);
}