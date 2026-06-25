using System.Security.Claims;
using assestment.Dto.Reservation;
using assestment.Interfaces.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace assestment.Controllers;

[Authorize]
public class ReservationController : Controller
{
    private readonly IReservationService _reservationService;

    public ReservationController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _reservationService.CreateReservation(dto, userId);

        if (!result.Success)
        {
            TempData["ReservationError"] = result.Message;

            // Si el rechazo es por falta de KYC, mandamos directo a verificar identidad
            if (result.Message == "Identity verification required before booking")
            {
                return RedirectToAction("Verify", "Kyc");
            }

            return RedirectToAction("Details", "Property", new { id = dto.PropertyId });
        }

        TempData["ReservationSuccess"] = "Tu reserva fue creada con éxito.";
        return RedirectToAction(nameof(MyReservations));
    }

    [HttpGet]
    public async Task<IActionResult> MyReservations()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _reservationService.GetReservationsByUser(userId);

        return View(result.Success ? result.Data : new List<Models.Reserva>());
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _reservationService.GetReservationById(id);

        if (!result.Success)
        {
            return NotFound(result.Message);
        }

        // Verifica que la reserva pertenece al usuario logueado
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (result.Data!.UsuarioId != userId)
        {
            return Forbid();
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _reservationService.CancelReservation(id, userId);

        TempData[result.Success ? "ReservationSuccess" : "ReservationError"] =
            result.Success ? "Reserva cancelada con éxito." : result.Message;

        return RedirectToAction(nameof(MyReservations));
    }
}