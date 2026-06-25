namespace assestment.Interfaces.Email;

public interface IEmailService
{
    Task<bool> SendWelcomeEmail(string toEmail, string nombre);
    Task<bool> SendReservationConfirmation(string toEmail, string nombre, string propertyTitle, DateOnly checkIn, DateOnly checkOut);
    Task<bool> SendPropertyDeactivatedAlert(string toEmail, string nombre, string propertyTitle, DateOnly checkIn, DateOnly checkOut);
    Task<bool> SendReservationCancellation(string toEmail, string nombre, string propertyTitle, DateOnly checkIn, DateOnly checkOut);
}