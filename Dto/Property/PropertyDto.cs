namespace assestment.Dto.Property;

public class PropertyDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Locate { get; set; }
    public decimal Latitude { get; set; }
    public decimal Length { get; set; }
    public decimal Rate { get; set; } 
    public bool IsActive { get; set; }  
    public string? UrlImagen { get; set; }
}