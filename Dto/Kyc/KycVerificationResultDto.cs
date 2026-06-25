namespace assestment.Dto.Kyc;

public class KycVerificationResultDto
{
    public bool Aprobado { get; set; }
    public List<string> Razones { get; set; } = new();
    public decimal ConfianzaOcr { get; set; }
}