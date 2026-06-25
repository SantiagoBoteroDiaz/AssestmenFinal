using assestment.Data;
using assestment.Dto.Property;
using assestment.Interfaces.Email;
using assestment.Interfaces.Property;
using assestment.Models;
using assestment.Response;
using Microsoft.EntityFrameworkCore;

namespace assestment.Services.Property;

public class PropertyService : IPropertyService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    
    public PropertyService(AppDbContext dbContext,  IEmailService emailService)
    {
        _emailService = emailService;
        _dbContext = dbContext;
    }
    public async Task<SystemResponse<Inmueble>> CreateProperty(PropertyDto property, Guid propertyId)
    {
        try
        {
            var newProperty = new Inmueble
            {
                PropietarioId = propertyId,
                Titulo = property.Title,
                Descripcion = property.Description,
                Ubicacion = property.Locate,
                Latitud = property.Latitude,
                Longitud = property.Length,
                TarifaPorNoche = property.Rate,
                Activo = true,
                UrlImagen = property.UrlImagen // <- nuevo
            };
            _dbContext.Inmuebles.Add(newProperty);
            await _dbContext.SaveChangesAsync();
            
            return new SystemResponse<Inmueble> { Success = true, Data = newProperty };
        }
        catch (Exception e)
        {
            return new SystemResponse<Inmueble> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<ICollection<Inmueble>>> GetAllProperties()
    {
        try
        {
            var properties = await _dbContext.Inmuebles.Where(x => x.Activo).ToListAsync();
            return new SystemResponse<ICollection<Inmueble>> { Success = true, Data = properties };
        }
        catch (Exception e)
        {
            return new SystemResponse<ICollection<Inmueble>> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<Inmueble>> GetPropertyById(Guid id)
    {
        try
        {
            var property = await _dbContext.Inmuebles.FindAsync(id);
            
            if (property == null)
            {
                return new SystemResponse<Inmueble> { Success = false, Message = "No property found" };
            }
            
            return new SystemResponse<Inmueble> { Success = true, Data = property };
        }
        catch (Exception e)
        {
           return new SystemResponse<Inmueble> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<Inmueble>> DesactiveProperty(Guid id, Guid ownerId)
    {
        try
        {
            var property = await _dbContext.Inmuebles.FirstOrDefaultAsync(x => x.Id == id);
            if (property == null)
            {
                return new SystemResponse<Inmueble> { Success = false, Message = "No property found" };
            }
            if (property.PropietarioId != ownerId)
            {
                return new SystemResponse<Inmueble> { Success = false, Message = "You are not allowed to deactivate this property" };
            }

            var reservasActivas = await _dbContext.Reservas
                .Include(r => r.Usuario)
                .Where(r => r.InmuebleId == id
                            && (r.Estado == "Pendiente" || r.Estado == "Confirmada")
                            && r.FechaFin >= DateOnly.FromDateTime(DateTime.UtcNow))
                .ToListAsync();

            property.Activo = false;

            foreach (var reserva in reservasActivas)
            {
                reserva.Estado = "Cancelada";
            }

            await _dbContext.SaveChangesAsync();

            foreach (var reserva in reservasActivas)
            {
                await _emailService.SendPropertyDeactivatedAlert(
                    reserva.Usuario.Email, reserva.Usuario.Nombres,
                    property.Titulo, reserva.FechaInicio, reserva.FechaFin);
            }

            return new SystemResponse<Inmueble> { Success = true, Data = property };
        }
        catch (Exception e)
        {
            return new SystemResponse<Inmueble> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<ICollection<Inmueble>>> GetAllPropertyByOwner(Guid ownerId)
    {
        try
        {
            var properties = await _dbContext.Inmuebles.Where(x => x.PropietarioId == ownerId).ToListAsync(); 
            
            return new SystemResponse<ICollection<Inmueble>> { Success = true, Data = properties };
        }
        catch (Exception e)
        {
            return new SystemResponse<ICollection<Inmueble>> { Success = false, Message = e.Message };
        }
    }
    public async Task<SystemResponse<ICollection<Inmueble>>> SearchAvailableProperties(string? ubicacion, DateOnly? fechaInicio, DateOnly? fechaFin)
    {
        try
        {
            var query = _dbContext.Inmuebles.Where(x => x.Activo);

            if (!string.IsNullOrWhiteSpace(ubicacion))
            {
                query = query.Where(x => x.Ubicacion.Contains(ubicacion));
            }

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                if (fechaFin.Value <= fechaInicio.Value)
                {
                    return new SystemResponse<ICollection<Inmueble>> { Success = false, Message = "End date must be after start date" };
                }

                // Excluir inmuebles que tengan una reserva activa que se solape con el rango buscado
                query = query.Where(inmueble => !_dbContext.Reservas.Any(reserva =>
                    reserva.InmuebleId == inmueble.Id &&
                    (reserva.Estado == "Pendiente" || reserva.Estado == "Confirmada") &&
                    fechaInicio.Value < reserva.FechaFin &&
                    fechaFin.Value > reserva.FechaInicio));
            }

            var properties = await query.ToListAsync();

            return new SystemResponse<ICollection<Inmueble>> { Success = true, Data = properties };
        }
        catch (Exception e)
        {
            return new SystemResponse<ICollection<Inmueble>> { Success = false, Message = e.Message };
        }
    }public async Task<SystemResponse<Inmueble>> UpdateProperty(Guid id, PropertyDto property, Guid ownerId)
    {
        try
        {
            var existing = await _dbContext.Inmuebles.FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null)
            {
                return new SystemResponse<Inmueble> { Success = false, Message = "No property found" };
            }

            if (existing.PropietarioId != ownerId)
            {
                return new SystemResponse<Inmueble> { Success = false, Message = "You are not allowed to edit this property" };
            }

            if (property.Rate <= 0)
            {
                return new SystemResponse<Inmueble> { Success = false, Message = "Rate must be greater than zero" };
            }

            existing.Titulo = property.Title;
            existing.Descripcion = property.Description;
            existing.Ubicacion = property.Locate;
            existing.Latitud = property.Latitude;
            existing.Longitud = property.Length;
            existing.TarifaPorNoche = property.Rate;
            existing.UrlImagen = property.UrlImagen;
            existing.Activo = property.IsActive; // <- nuevo

            await _dbContext.SaveChangesAsync();

            return new SystemResponse<Inmueble> { Success = true, Data = existing };
        }
        catch (Exception e)
        {
            return new SystemResponse<Inmueble> { Success = false, Message = e.Message };
        }
    }
}