namespace assestment.Dto.Property;

public class DashboardResumenDto
{
    public decimal IngresosTotales { get; set; }
    public decimal TasaOcupacionPromedio { get; set; }
    public int TotalReservas { get; set; }
    public List<PropertyMetricsDto> PorInmueble { get; set; } = new();
}