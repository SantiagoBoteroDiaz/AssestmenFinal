namespace assestment.Dto.Reservation;

public class ReservationDto
{
    public Guid PropertyId { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
}