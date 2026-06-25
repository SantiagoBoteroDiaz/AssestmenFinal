using assestment.Dto.Reservation;
using assestment.Models;
using assestment.Response;

namespace assestment.Interfaces.Reservation;

public interface IReservationService
{
    Task<SystemResponse<Reserva>> CreateReservation(ReservationDto reservation, Guid userId);
    Task<SystemResponse<ICollection<Reserva>>> GetReservationsByUser(Guid userId);
    Task<SystemResponse<Reserva>> GetReservationById(Guid id);
    Task<SystemResponse<bool>> CancelReservation(Guid id, Guid userId);
}