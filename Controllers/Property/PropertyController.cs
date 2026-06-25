using System.Security.Claims;
using assestment.Dto.Property;
using assestment.Interfaces.Property;
using assestment.Models;
using assestment.ViewModels.Property;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace assestment.Controllers;

public class PropertyController : Controller
{
    private readonly IPropertyService _propertyService;

    public PropertyController(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index(string? ubicacion, DateOnly? fechaInicio, DateOnly? fechaFin)
    {
        try
        {
            var result = await _propertyService.SearchAvailableProperties(ubicacion, fechaInicio, fechaFin);

            var viewModel = new CatalogViewModel
            {
                Properties = result.Success ? result.Data! : new List<Inmueble>(),
                Ubicacion = ubicacion,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                ErrorMessage = result.Success ? null : result.Message
            };

            return View(viewModel);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    // GET: /Property/Details/{id} — acceso público, el huésped ve el inmueble sin loguearse
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _propertyService.GetPropertyById(id);

        if (!result.Success)
        {
            return NotFound(result.Message);
        }

        return View(result.Data);
    }

    // GET: /Property/Create — solo propietarios
    [Authorize(Roles = "Propietario")]
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Property/Create
    [Authorize(Roles = "Propietario")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _propertyService.CreateProperty(dto, ownerId);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Could not create property");
            return View(dto);
        }

        return RedirectToAction(nameof(MyProperties));
    }

    // GET: /Property/MyProperties — listado de inmuebles del propietario logueado
    [Authorize(Roles = "Propietario")]
    [HttpGet]
    public async Task<IActionResult> MyProperties()
    {
        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _propertyService.GetAllPropertyByOwner(ownerId);

        return View(result.Data);
    }

    [Authorize(Roles = "Propietario")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _propertyService.DesactiveProperty(id, ownerId);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(MyProperties));
    }
    
    // POST: /Property/Edit/{id}
    [Authorize(Roles = "Propietario")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PropertyDto dto)
    {
        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _propertyService.UpdateProperty(id, dto, ownerId);

        if (!result.Success)
        {
            TempData["EditError"] = result.Message;
            ViewBag.PropertyId = id;
            return View(dto);
        }

        TempData["EditSuccess"] = "Inmueble actualizado con éxito.";
        return RedirectToAction(nameof(MyProperties));
    }
    
    // GET: /Property/Edit/{id}
    [Authorize(Roles = "Propietario")]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _propertyService.GetPropertyById(id);

        if (!result.Success)
        {
            return NotFound(result.Message);
        }

        var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (result.Data!.PropietarioId != ownerId)
        {
            return Forbid();
        }

        var dto = new PropertyDto
        {
            Title = result.Data.Titulo,
            Description = result.Data.Descripcion,
            Locate = result.Data.Ubicacion,
            Latitude = result.Data.Latitud ?? 0,
            Length = result.Data.Longitud ?? 0,
            Rate = result.Data.TarifaPorNoche,
            UrlImagen = result.Data.UrlImagen,
            IsActive = result.Data.Activo // <- nuevo
        };

        ViewBag.PropertyId = id;
        return View(dto);
    }
}