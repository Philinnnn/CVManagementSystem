namespace CVManagementSystem.Services.Salesforce.Dtos;

public class SalesforceSyncRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
}