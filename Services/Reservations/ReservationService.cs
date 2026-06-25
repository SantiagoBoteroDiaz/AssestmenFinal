using assestment.Data;
using assestment.Dto.Reservation;
using assestment.Interfaces.Email;
using assestment.Interfaces.Reservation;
using assestment.Models;
using assestment.Response;
using Microsoft.EntityFrameworkCore;

namespace assestment.Services.Reservation;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    
    public ReservationService(AppDbContext dbContext, IEmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    public async Task<SystemResponse<Reserva>> CreateReservation(ReservationDto reservation, Guid userId)
    {
        try
        {
            //  Validar que las fechas tengan sentido
            if (reservation.CheckOut <= reservation.CheckIn)
            {
                return new SystemResponse<Reserva> { Success = false, Message = "Check-out date must be after check-in date" };
            }

            if (reservation.CheckIn < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return new SystemResponse<Reserva> { Success = false, Message = "Check-in date cannot be in the past" };
            }

            //  Validar KYC aprobado antes de la primera reserva
            var kyc = await _dbContext.KycVerifications
                .FirstOrDefaultAsync(x => x.UsuarioId == userId);

            if (kyc == null || kyc.Estado != "Aprobado")
            {
                return new SystemResponse<Reserva> { Success = false, Message = "Identity verification required before booking" };
            }

            //  Validar que el inmueble exista y esté activo
            var property = await _dbContext.Inmuebles
                .FirstOrDefaultAsync(x => x.Id == reservation.PropertyId);

            if (property == null)
            {
                return new SystemResponse<Reserva> { Success = false, Message = "No property found" };
            }

            if (!property.Activo)
            {
                return new SystemResponse<Reserva> { Success = false, Message = "This property is not available for booking" };
            }

            // Validar que no haya solapamiento con reservas activas (Pendiente o Confirmada)
            var overlaps = await _dbContext.Reservas.AnyAsync(x =>
                x.InmuebleId == reservation.PropertyId &&
                (x.Estado == "Pendiente" || x.Estado == "Confirmada") &&
                reservation.CheckIn < x.FechaFin &&
                reservation.CheckOut > x.FechaInicio);

            if (overlaps)
            {
                return new SystemResponse<Reserva> { Success = false, Message = "Property is already booked for the selected dates" };
            }

            // 5. Calcular el precio en el backend, nunca confiar en lo que mande el cliente
            var noches = reservation.CheckOut.DayNumber - reservation.CheckIn.DayNumber;
            var precioTotal = property.TarifaPorNoche * noches;

            var newReservation = new Reserva
            {
                InmuebleId = reservation.PropertyId,
                UsuarioId = userId,
                FechaInicio = reservation.CheckIn,
                FechaFin = reservation.CheckOut,
                PrecioTotal = precioTotal,
                Estado = "Confirmada"
                // HoraCheckIn / HoraCheckOut no se asignan: ya tienen default fijo en la entidad/DB (14:00 / 12:00)
            };

            _dbContext.Reservas.Add(newReservation);
            await _dbContext.SaveChangesAsync();

            var usuario = await _dbContext.Usuarios.FindAsync(userId); // <- esta línea faltaba
            await _emailService.SendReservationConfirmation(usuario!.Email, usuario.Nombres, property.Titulo, reservation.CheckIn, reservation.CheckOut);

            return new SystemResponse<Reserva> { Success = true, Data = newReservation };
        }
        catch (DbUpdateException e) when (e.InnerException?.Message.Contains("excl_reservas_solapadas") == true)
        {
            // Red de seguridad: si dos requests concurrentes pasan la validación anterior
            // al mismo tiempo, la constraint de Postgres rechaza el segundo insert.
            return new SystemResponse<Reserva> { Success = false, Message = "Property is already booked for the selected dates" };
        }
        catch (Exception e)
        {
            return new SystemResponse<Reserva> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<ICollection<Reserva>>> GetReservationsByUser(Guid userId)
    {
        try
        {
            var reservations = await _dbContext.Reservas
                .Where(x => x.UsuarioId == userId)
                .Include(x => x.Inmueble)
                .ToListAsync();

            return new SystemResponse<ICollection<Reserva>> { Success = true, Data = reservations };
        }
        catch (Exception e)
        {
            return new SystemResponse<ICollection<Reserva>> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<Reserva>> GetReservationById(Guid id)
    {
        try
        {
            var reservation = await _dbContext.Reservas
                .Include(x => x.Inmueble)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (reservation == null)
            {
                return new SystemResponse<Reserva> { Success = false, Message = "No reservation found" };
            }

            return new SystemResponse<Reserva> { Success = true, Data = reservation };
        }
        catch (Exception e)
        {
            return new SystemResponse<Reserva> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<bool>> CancelReservation(Guid id, Guid userId)
    {
        try
        {
            var reservation = await _dbContext.Reservas
                .Include(r => r.Inmueble)
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == userId);

            if (reservation == null)
            {
                return new SystemResponse<bool> { Success = false, Message = "No reservation found" };
            }

            if (reservation.Estado == "Cancelada" || reservation.Estado == "Completada")
            {
                return new SystemResponse<bool> { Success = false, Message = "This reservation cannot be cancelled" };
            }

            reservation.Estado = "Cancelada";
            await _dbContext.SaveChangesAsync();

            await _emailService.SendReservationCancellation(
                reservation.Usuario.Email, reservation.Usuario.Nombres,
                reservation.Inmueble.Titulo, reservation.FechaInicio, reservation.FechaFin);

            return new SystemResponse<bool> { Success = true, Data = true };
        }
        catch (Exception e)
        {
            return new SystemResponse<bool> { Success = false, Message = e.Message };
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

            if (reservasActivas.Count > 0)
            {
                foreach (var reserva in reservasActivas)
                {
                    reserva.Estado = "Cancelada";
                }
            }

            await _dbContext.SaveChangesAsync(); 

            if (reservasActivas.Count > 0)
            {
                foreach (var reserva in reservasActivas)
                {
                    await _emailService.SendPropertyDeactivatedAlert(
                        reserva.Usuario.Email, reserva.Usuario.Nombres,
                        property.Titulo, reserva.FechaInicio, reserva.FechaFin);
                }
            }

            return new SystemResponse<Inmueble> { Success = true, Data = property };
        }
        catch (Exception e)
        {
            return new SystemResponse<Inmueble> { Success = false, Message = e.Message };
        }
    }
}
