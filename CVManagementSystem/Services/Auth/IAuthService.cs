namespace CVManagementSystem.Services.Auth;

using Models.Identity;

public interface IAuthService
{
    Task<(bool Success, string? Error, User? User)> RegisterAsync(string login, string email, string password, string fullname);
    Task<(bool Success, string? Error, User? User)> ValidateCredentialsAsync(string login, string password);
}