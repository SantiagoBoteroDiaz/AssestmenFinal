namespace assestment.Dto.Property;

public class PropertyMetricsDto
{
    public Guid PropertyId { get; set; }
    public string Titulo { get; set; }
    public decimal Ingresos { get; set; }
    public int NochesReservadas { get; set; }
    public int NochesDisponibles { get; set; }
    public decimal TasaOcupacion { get; set; } // 0 a 1
    public int TotalReservas { get; set; }
}