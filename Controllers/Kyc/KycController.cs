using System.Security.Claims;
using assestment.Interfaces.Kyc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace assestment.Controllers;

[Authorize]
public class KycController : Controller
{
    private readonly IAiExtractionService _aiExtractionService;
    private readonly IKycService _kycService;
    private const long MaxFileSizeBytes = 5_000_000; // 5MB

    public KycController(IAiExtractionService aiExtractionService, IKycService kycService)
    {
        _aiExtractionService = aiExtractionService;
        _kycService = kycService;
    }

    [HttpGet]
    public async Task<IActionResult> Verify()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var existing = await _kycService.GetByUserId(userId);

        return View(existing.Success ? existing.Data : null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(IFormFile documentImage)
    {
        if (documentImage == null || documentImage.Length == 0)
        {
            TempData["KycError"] = "Debes seleccionar una imagen de tu documento";
            return RedirectToAction(nameof(Verify));
        }

        if (documentImage.Length > MaxFileSizeBytes)
        {
            TempData["KycError"] = "La imagen no debe superar los 5MB";
            return RedirectToAction(nameof(Verify));
        }

        if (!documentImage.ContentType.StartsWith("image/"))
        {
            TempData["KycError"] = "El archivo debe ser una imagen";
            return RedirectToAction(nameof(Verify));
        }

        using var memoryStream = new MemoryStream();
        await documentImage.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        var extractionResult = await _aiExtractionService.ExtractDocumentData(imageBytes, documentImage.ContentType);

        if (!extractionResult.Success)
        {
            TempData["KycError"] = extractionResult.Message;
            return RedirectToAction(nameof(Verify));
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var kycResult = await _kycService.ValidateAndPersist(userId, extractionResult.Data!);

        if (!kycResult.Success)
        {
            TempData["KycError"] = kycResult.Message ?? "No pudimos completar la verificación";
            return RedirectToAction(nameof(Verify));
        }

        if (kycResult.Data!.Estado == "Aprobado")
        {
            TempData["KycSuccess"] = "Tu identidad fue verificada con éxito.";
        }
        else
        {
            TempData["KycError"] = "No pudimos verificar tu identidad con los datos proporcionados. Revisa que tus datos de registro coincidan con tu documento.";
        }

        return RedirectToAction(nameof(Verify));
    }
}