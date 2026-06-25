using assestment.Dto.Property;
using assestment.Response;

namespace assestment.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<SystemResponse<DashboardResumenDto>> GetResumen(Guid ownerId, DateOnly fechaInicio, DateOnly fechaFin);
    Task<SystemResponse<byte[]>> GenerateExcelReport(Guid ownerId, Guid? propertyId);
}