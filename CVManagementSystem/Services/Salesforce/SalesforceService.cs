namespace CVManagementSystem.Services.Salesforce;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Data;
using Dtos;
using Microsoft.Extensions.Configuration;
using Common;
using Models.Identity;

public class SalesforceService(IHttpClientFactory httpClientFactory, IConfiguration configuration, AppDbContext db) : ISalesforceService
{
    public async Task<OperationResult> SyncUserAsync(User user, SalesforceSyncRequest request)
    {
        var client = httpClientFactory.CreateClient();

        var tokenResult = await GetAccessTokenAsync(client);
        if (!tokenResult.Success)
            return OperationResult.Fail(tokenResult.Error);

        var (accessToken, instanceUrl) = tokenResult.Value;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var nameParts = user.Fullname.Split(' ', 2);
        var firstName = nameParts.Length > 0 ? nameParts[0] : user.Fullname;
        var lastName = nameParts.Length > 1 ? nameParts[1] : user.Fullname;
        
        if (!string.IsNullOrEmpty(user.SalesforceContactId))
        {
            var updatePayload = new
            {
                FirstName = firstName,
                LastName = lastName,
                Email = user.Email,
                Phone = request.Phone,
                Title = request.JobTitle,
                Description = request.Notes
            };

            var updateResponse = await client.PatchAsync(
                $"{instanceUrl}/services/data/v60.0/sobjects/Contact/{user.SalesforceContactId}",
                new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json"));

            if (!updateResponse.IsSuccessStatusCode)
                return OperationResult.Fail($"Salesforce Contact update failed: {await updateResponse.Content.ReadAsStringAsync()}");

            user.SalesforceSyncedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return OperationResult.Ok();
        }
        
        var accountPayload = new
        {
            Name = string.IsNullOrWhiteSpace(request.CompanyName) ? $"{user.Fullname} — Personal" : request.CompanyName,
            Phone = request.Phone
        };

        var accountResponse = await client.PostAsync(
            $"{instanceUrl}/services/data/v60.0/sobjects/Account",
            new StringContent(JsonSerializer.Serialize(accountPayload), Encoding.UTF8, "application/json"));

        if (!accountResponse.IsSuccessStatusCode)
            return OperationResult.Fail($"Salesforce Account creation failed: {await accountResponse.Content.ReadAsStringAsync()}");

        using var accountJson = JsonDocument.Parse(await accountResponse.Content.ReadAsStringAsync());
        var accountId = accountJson.RootElement.GetProperty("id").GetString();

        var contactPayload = new
        {
            AccountId = accountId,
            FirstName = firstName,
            LastName = lastName,
            Email = user.Email,
            Phone = request.Phone,
            Title = request.JobTitle,
            Description = request.Notes
        };

        var contactResponse = await client.PostAsync(
            $"{instanceUrl}/services/data/v60.0/sobjects/Contact",
            new StringContent(JsonSerializer.Serialize(contactPayload), Encoding.UTF8, "application/json"));

        if (!contactResponse.IsSuccessStatusCode)
            return OperationResult.Fail($"Salesforce Contact creation failed: {await contactResponse.Content.ReadAsStringAsync()}");

        using var contactJson = JsonDocument.Parse(await contactResponse.Content.ReadAsStringAsync());
        var contactId = contactJson.RootElement.GetProperty("id").GetString();

        user.SalesforceContactId = contactId;
        user.SalesforceSyncedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return OperationResult.Ok();
    }

    private async Task<OperationResult<(string AccessToken, string InstanceUrl)>> GetAccessTokenAsync(HttpClient client)
    {
        var sfConfig = configuration.GetSection("Salesforce");
        var domain = sfConfig["MyDomainUrl"]!;
        
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = sfConfig["ClientId"]!,
            ["client_secret"] = sfConfig["ClientSecret"]!
        };
        
        var response = await client.PostAsync($"{domain}/services/oauth2/token", new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
            return OperationResult<(string, string)>.Fail($"Salesforce authentication failed: {body}");
        
        using var json = JsonDocument.Parse(body);
        var accessToken = json.RootElement.GetProperty("access_token").GetString()!;
        var instanceUrl = json.RootElement.GetProperty("instance_url").GetString()!;
        
        return OperationResult<(string, string)>.Ok((accessToken, instanceUrl));
    }
}