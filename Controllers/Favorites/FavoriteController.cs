using System.Security.Claims;
using assestment.Interfaces.Favorite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace assestment.Controllers;

[Authorize]
public class FavoriteController : Controller
{
    private readonly IFavoriteService _favoriteService;

    public FavoriteController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _favoriteService.GetFavoritesByUser(userId);

        return View(result.Success ? result.Data : new List<assestment.Models.Inmueble>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid propertyId, string? returnUrl)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _favoriteService.AddFavorite(userId, propertyId);

        if (!result.Success)
        {
            TempData["FavoriteError"] = result.Message;
        }

        return RedirectFromFavoriteAction(returnUrl, propertyId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid propertyId, string? returnUrl)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _favoriteService.RemoveFavorite(userId, propertyId);

        if (!result.Success)
        {
            TempData["FavoriteError"] = result.Message;
        }

        return RedirectFromFavoriteAction(returnUrl, propertyId);
    }

    private IActionResult RedirectFromFavoriteAction(string? returnUrl, Guid propertyId)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Details", "Property", new { id = propertyId });
    }
}