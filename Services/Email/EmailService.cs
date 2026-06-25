using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using assestment.Interfaces.Email;

namespace assestment.Services.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> SendWelcomeEmail(string toEmail, string nombre)
    {
        var subject = "Bienvenido a Rentas Cortas";
        var body = $"""
            <h2>¡Hola {nombre}!</h2>
            <p>Tu cuenta fue creada con éxito. Ya puedes explorar el catálogo de alojamientos.</p>
            <p>Recuerda que necesitarás verificar tu identidad antes de completar tu primera reserva.</p>
            """;

        return await SendEmail(toEmail, subject, body);
    }

    public async Task<bool> SendReservationConfirmation(string toEmail, string nombre, string propertyTitle, DateOnly checkIn, DateOnly checkOut)
    {
        var subject = "Confirmación de tu reserva";
        var body = $"""
            <h2>¡Hola {nombre}!</h2>
            <p>Tu reserva en <strong>{propertyTitle}</strong> fue confirmada.</p>
            <p><strong>Check-in:</strong> {checkIn:dd/MM/yyyy} a las 2:00 PM</p>
            <p><strong>Check-out:</strong> {checkOut:dd/MM/yyyy} a las 12:00 PM</p>
            """;

        return await SendEmail(toEmail, subject, body);
    }

    public async Task<bool> SendPropertyDeactivatedAlert(string toEmail, string nombre, string propertyTitle, DateOnly checkIn, DateOnly checkOut)
    {
        var subject = "Aviso importante sobre tu reserva";
        var body = $"""
            <h2>Hola {nombre}</h2>
            <p>El inmueble <strong>{propertyTitle}</strong> de tu reserva del {checkIn:dd/MM/yyyy} al {checkOut:dd/MM/yyyy}
            fue desactivado por el propietario.</p>
            <p>Por favor contáctanos para revisar las opciones disponibles.</p>
            """;

        return await SendEmail(toEmail, subject, body);
    }

    private async Task<bool> SendEmail(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_configuration["Smtp:FromName"], _configuration["Smtp:Username"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration["Smtp:Host"], int.Parse(_configuration["Smtp:Port"]!), MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_configuration["Smtp:Username"], _configuration["Smtp:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception)
        {
            return false; // no rompemos el flujo principal si falla el correo
        }
    }
    public async Task<bool> SendReservationCancellation(string toEmail, string nombre, string propertyTitle, DateOnly checkIn, DateOnly checkOut)
    {
        var subject = "Tu reserva fue cancelada";
        var body = $"""
                    <h2>Hola {nombre}</h2>
                    <p>Confirmamos la cancelación de tu reserva en <strong>{propertyTitle}</strong>
                    para el periodo del {checkIn:dd/MM/yyyy} al {checkOut:dd/MM/yyyy}.</p>
                    """;

        return await SendEmail(toEmail, subject, body);
    }
}