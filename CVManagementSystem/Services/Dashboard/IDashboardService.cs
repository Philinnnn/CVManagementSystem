namespace CVManagementSystem.Services.Dashboard;

using Dtos;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync();
}