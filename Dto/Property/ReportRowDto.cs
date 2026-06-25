namespace assestment.Dto.Property;

public class ReportRowDto
{
    public string Inmueble { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public decimal PrecioTotal { get; set; }
    public string Estado { get; set; }
    public string HuespedNombre { get; set; }
    public string HuespedEmail { get; set; }
    public string HuespedDocumento { get; set; }
}