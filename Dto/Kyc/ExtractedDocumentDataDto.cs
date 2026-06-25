namespace assestment.Dto.Kyc;

public class ExtractedDocumentDataDto
{
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? NumeroDocumento { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public decimal Confianza { get; set; }
    public bool DocumentoLegible { get; set; } 
    
}