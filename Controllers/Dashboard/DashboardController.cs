using System.Security.Claims;
using assestment.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace assestment.Controllers;

[Authorize(Roles = "Propietario")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? fechaInicio, DateOnly? fechaFin)
    {
        var inicio = fechaInicio ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var fin = fechaFin ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _dashboardService.GetResumen(ownerId, inicio, fin);

        ViewBag.FechaInicio = inicio;
        ViewBag.FechaFin = fin;

        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(new Dto.Property.DashboardResumenDto());
        }

        return View(result.Data);
    }
    [HttpGet]
    public async Task<IActionResult> ExportExcel(Guid? propertyId)
    {
        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _dashboardService.GenerateExcelReport(ownerId, propertyId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var fileName = propertyId.HasValue
            ? $"reporte_inmueble_{DateTime.UtcNow:yyyyMMdd}.xlsx"
            : $"reporte_portafolio_{DateTime.UtcNow:yyyyMMdd}.xlsx";

        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}