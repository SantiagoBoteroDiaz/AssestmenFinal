using System.Globalization;
using System.Text;
using assestment.Data;
using assestment.Dto.Kyc;
using assestment.Interfaces.Kyc;
using assestment.Models;
using assestment.Response;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace assestment.Services.Kyc;

public class KycService : IKycService
{
    private readonly AppDbContext _dbContext;
    private const decimal ConfianzaMinima = 0.80m;

    public KycService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemResponse<KycVerification>> ValidateAndPersist(Guid userId, ExtractedDocumentDataDto extracted)
    {
        try
        {
            var usuario = await _dbContext.Usuarios.FirstOrDefaultAsync(x => x.Id == userId);
            if (usuario == null)
            {
                return new SystemResponse<KycVerification> { Success = false, Message = "User not found" };
            }

            var razones = new List<string>();

            // 1. Validar legibilidad y confianza del OCR antes de comparar nada
            if (!extracted.DocumentoLegible)
            {
                razones.Add("documento_no_legible");
            }

            if (extracted.Confianza < ConfianzaMinima)
            {
                razones.Add("baja_confianza_ocr");
            }

            // 2. Comparar nombre completo (normalizado, tolerante a diferencias menores de OCR)
            var nombreExtraido = Normalizar($"{extracted.Nombres} {extracted.Apellidos}");
            var nombreUsuario = Normalizar($"{usuario.Nombres} {usuario.Apellidos}");
            var nombreCoincide = nombreExtraido == nombreUsuario;

            if (!nombreCoincide)
            {
                razones.Add("nombre_no_coincide");
            }

            // 3. Comparar número de documento (estricto, sin tolerancia)
            var documentoCoincide = Normalizar(extracted.NumeroDocumento ?? "") == Normalizar(usuario.NumeroDocumento);

            if (!documentoCoincide)
            {
                razones.Add("documento_no_coincide");
            }

            // 4. Comparar fecha de nacimiento (estricta)
            var fechaNacimientoCoincide = extracted.FechaNacimiento.HasValue
                && extracted.FechaNacimiento.Value == usuario.FechaNacimiento;

            if (!fechaNacimientoCoincide)
            {
                razones.Add("fecha_nacimiento_no_coincide");
            }

            var aprobado = razones.Count == 0;

            // 5. Buscar verificación previa (relación 1:1) o crear una nueva
            var kyc = await _dbContext.KycVerifications.FirstOrDefaultAsync(x => x.UsuarioId == userId);

            if (kyc == null)
            {
                kyc = new KycVerification { UsuarioId = userId };
                _dbContext.KycVerifications.Add(kyc);
            }

            kyc.Estado = aprobado ? "Aprobado" : "Rechazado";
            kyc.NombresExtraidos = extracted.Nombres;
            kyc.ApellidosExtraidos = extracted.Apellidos;
            kyc.NumeroDocumentoExtraido = extracted.NumeroDocumento;
            kyc.FechaNacimientoExtraida = extracted.FechaNacimiento;
            kyc.ConfianzaOcr = extracted.Confianza;
            kyc.NombreCoincide = nombreCoincide;
            kyc.DocumentoCoincide = documentoCoincide;
            kyc.FechaNacimientoCoincide = fechaNacimientoCoincide;
            kyc.Razones = JsonSerializer.Serialize(razones);
            kyc.FechaVerificacion = DateTime.UtcNow;

            // 6. Sincronizar el campo de acceso rápido en Usuario
            usuario.KycAprobado = aprobado;

            await _dbContext.SaveChangesAsync();

            return new SystemResponse<KycVerification> { Success = true, Data = kyc };
        }
        catch (Exception e)
        {
            return new SystemResponse<KycVerification> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<KycVerification>> GetByUserId(Guid userId)
    {
        try
        {
            var kyc = await _dbContext.KycVerifications.FirstOrDefaultAsync(x => x.UsuarioId == userId);

            if (kyc == null)
            {
                return new SystemResponse<KycVerification> { Success = false, Message = "No verification found for this user" };
            }

            return new SystemResponse<KycVerification> { Success = true, Data = kyc };
        }
        catch (Exception e)
        {
            return new SystemResponse<KycVerification> { Success = false, Message = e.Message };
        }
    }

    private static string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var sinTildes = QuitarTildes(texto.Trim().ToUpperInvariant());
        return string.Join(" ", sinTildes.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string QuitarTildes(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}