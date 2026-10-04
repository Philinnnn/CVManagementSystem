namespace CVManagementSystem.Services.Integrations;

public interface IDropboxService
{
    string GetAuthorizeUrl(string redirectUri);
    Task ExchangeCodeAsync(string code, string redirectUri);
    Task UploadJsonAsync(string fileName, string jsonContent);
    bool IsConnected { get; }
}