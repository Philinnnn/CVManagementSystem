namespace CVManagementSystem.Services.Salesforce;

using Dtos;
using Common;
using Models.Identity;

public interface ISalesforceService
{
    Task<OperationResult> SyncUserAsync(User user, SalesforceSyncRequest request);
}