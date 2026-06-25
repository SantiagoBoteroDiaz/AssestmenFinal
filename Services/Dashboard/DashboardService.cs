using assestment.Data;
using assestment.Dto.Property;
using assestment.Interfaces.Dashboard;
using assestment.Response;
using Microsoft.EntityFrameworkCore;

namespace assestment.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;

    public DashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemResponse<DashboardResumenDto>> GetResumen(Guid ownerId, DateOnly fechaInicio, DateOnly fechaFin)
    {
        try
        {
            if (fechaFin <= fechaInicio)
            {
                return new SystemResponse<DashboardResumenDto> { Success = false, Message = "End date must be after start date" };
            }

            var nochesDelPeriodo = fechaFin.DayNumber - fechaInicio.DayNumber;

            var inmuebles = await _dbContext.Inmuebles
                .Where(i => i.PropietarioId == ownerId)
                .ToListAsync();

            var porInmueble = new List<PropertyMetricsDto>();

            foreach (var inmueble in inmuebles)
            {
                var reservas = await _dbContext.Reservas
                    .Where(r => r.InmuebleId == inmueble.Id
                        && (r.Estado == "Confirmada" || r.Estado == "Completada")
                        && r.FechaInicio < fechaFin
                        && r.FechaFin > fechaInicio)
                    .ToListAsync();

                var ingresos = reservas.Sum(r => r.PrecioTotal);

                // Noches reservadas dentro del periodo (recorta la reserva al rango si se solapa parcialmente)
                var nochesReservadas = reservas.Sum(r =>
                {
                    var inicio = r.FechaInicio > fechaInicio ? r.FechaInicio : fechaInicio;
                    var fin = r.FechaFin < fechaFin ? r.FechaFin : fechaFin;
                    return Math.Max(0, fin.DayNumber - inicio.DayNumber);
                });

                porInmueble.Add(new PropertyMetricsDto
                {
                    PropertyId = inmueble.Id,
                    Titulo = inmueble.Titulo,
                    Ingresos = ingresos,
                    NochesReservadas = nochesReservadas,
                    NochesDisponibles = nochesDelPeriodo,
                    TasaOcupacion = nochesDelPeriodo > 0 ? (decimal)nochesReservadas / nochesDelPeriodo : 0,
                    TotalReservas = reservas.Count
                });
            }

            var resumen = new DashboardResumenDto
            {
                IngresosTotales = porInmueble.Sum(p => p.Ingresos),
                TotalReservas = porInmueble.Sum(p => p.TotalReservas),
                TasaOcupacionPromedio = porInmueble.Count > 0 ? porInmueble.Average(p => p.TasaOcupacion) : 0,
                PorInmueble = porInmueble.OrderByDescending(p => p.Ingresos).ToList()
            };

            return new SystemResponse<DashboardResumenDto> { Success = true, Data = resumen };
        }
        catch (Exception e)
        {
            return new SystemResponse<DashboardResumenDto> { Success = false, Message = e.Message };
        }
    }
    
    public async Task<SystemResponse<byte[]>> GenerateExcelReport(Guid ownerId, Guid? propertyId)
{
    try
    {
        var query = _dbContext.Reservas
            .Include(r => r.Inmueble)
            .Include(r => r.Usuario)
            .Where(r => r.Inmueble.PropietarioId == ownerId);

        if (propertyId.HasValue)
        {
            query = query.Where(r => r.InmuebleId == propertyId.Value);
        }

        var reservas = await query
            .OrderBy(r => r.FechaInicio)
            .ToListAsync();

        var filas = reservas.Select(r => new ReportRowDto
        {
            Inmueble = r.Inmueble.Titulo,
            FechaInicio = r.FechaInicio,
            FechaFin = r.FechaFin,
            PrecioTotal = r.PrecioTotal,
            Estado = r.Estado,
            HuespedNombre = $"{r.Usuario.Nombres} {r.Usuario.Apellidos}",
            HuespedEmail = r.Usuario.Email,
            HuespedDocumento = r.Usuario.NumeroDocumento
        }).ToList();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.Worksheets.Add("Reservas");

        var headers = new[] { "Inmueble", "Check-in", "Check-out", "Precio total", "Estado", "Huésped", "Email", "Documento" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
            sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
        }

        for (var i = 0; i < filas.Count; i++)
        {
            var row = i + 2;
            var f = filas[i];
            sheet.Cell(row, 1).Value = f.Inmueble;
            sheet.Cell(row, 2).Value = f.FechaInicio.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy";
            sheet.Cell(row, 3).Value = f.FechaFin.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy";
            sheet.Cell(row, 4).Value = f.PrecioTotal;
            sheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0";
            sheet.Cell(row, 5).Value = f.Estado;
            sheet.Cell(row, 6).Value = f.HuespedNombre;
            sheet.Cell(row, 7).Value = f.HuespedEmail;
            sheet.Cell(row, 8).Value = f.HuespedDocumento;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new SystemResponse<byte[]> { Success = true, Data = stream.ToArray() };
    }
    catch (Exception e)
    {
        return new SystemResponse<byte[]> { Success = false, Message = e.Message };
    }
}
}