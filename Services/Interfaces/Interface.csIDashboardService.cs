using SistemaDeVentas.DTOs.Dashboard;

namespace SistemaDeVentas.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> ObtenerDashboardAsync();
    }
}
