using assestment.Models;

namespace assestment.ViewModels.Property;

public class CatalogViewModel
{
    public ICollection<Inmueble> Properties { get; set; } = new List<Inmueble>();
    public string? Ubicacion { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; } 
    public string? ErrorMessage { get; set; } // <- nuevo
}