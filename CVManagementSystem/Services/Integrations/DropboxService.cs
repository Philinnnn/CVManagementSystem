namespace CVManagementSystem.Services.Integrations;

using System.Text;
using System.Text.Json;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Models.Integrations;

public class DropboxService(IHttpClientFactory httpClientFactory, IConfiguration configuration, AppDbContext db) : IDropboxService
{
    private readonly string clientId = configuration["Dropbox:ClientId"]!;
    private readonly string clientSecret = configuration["Dropbox:ClientSecret"]!;

    public bool IsConnected => db.DropboxCredentials.Any();

    public string GetAuthorizeUrl(string redirectUri) =>
        $"https://www.dropbox.com/oauth2/authorize?client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&token_access_type=offline";

    public async Task ExchangeCodeAsync(string code, string redirectUri)
    {
        var client = httpClientFactory.CreateClient();

        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri
        };

        var response = await client.PostAsync("https://api.dropboxapi.com/oauth2/token", new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(body);
        var refreshToken = json.RootElement.GetProperty("refresh_token").GetString()!;

        var existing = await db.DropboxCredentials.FirstOrDefaultAsync();
        if (existing is null)
        {
            db.DropboxCredentials.Add(new DropboxCredential { RefreshToken = refreshToken });
        }
        else
        {
            existing.RefreshToken = refreshToken;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task UploadJsonAsync(string fileName, string jsonContent)
    {
        var credential = await db.DropboxCredentials.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Dropbox is not connected yet.");

        var client = httpClientFactory.CreateClient();
        var refreshForm = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = credential.RefreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        var refreshResponse = await client.PostAsync("https://api.dropboxapi.com/oauth2/token", new FormUrlEncodedContent(refreshForm));
        refreshResponse.EnsureSuccessStatusCode();

        using var refreshJson = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        var accessToken = refreshJson.RootElement.GetProperty("access_token").GetString()!;

        var uploadArgs = JsonSerializer.Serialize(new
        {
            path = $"/{fileName}",
            mode = "add",
            autorename = true,
            mute = false
        });

        var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8)
        };
        uploadRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
        uploadRequest.Headers.Add("Dropbox-API-Arg", uploadArgs);
        uploadRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var uploadResponse = await client.SendAsync(uploadRequest);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync();

        if (!uploadResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Dropbox upload failed: {uploadBody}");
    }
}