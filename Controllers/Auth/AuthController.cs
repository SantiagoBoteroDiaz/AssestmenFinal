using System.Security.Claims;
using assestment.Dto.Login;
using assestment.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace assestment.Controllers;

public class AuthController : Controller
{
    private readonly IAccesService _accesService;

    public AuthController(IAccesService accesService)
    {
        _accesService = accesService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AuthUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _accesService.Login(dto);

        if (!result.Success)
        {
            TempData["LoginError"] = result.Message ?? "Invalid credentials";
            return View(dto);
        }

        var usuario = result.Data!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, $"{usuario.Nombres} {usuario.Apellidos}"),
            new(ClaimTypes.Role, usuario.Rol),
            new("kyc_aprobado", usuario.KycAprobado.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        if (usuario.Rol == "Propietario")
        {
            return RedirectToAction("MyProperties", "Property");
        }

        return RedirectToAction("Index", "Property");

    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _accesService.Register(dto);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Could not register");
            return View(dto);
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Property");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}