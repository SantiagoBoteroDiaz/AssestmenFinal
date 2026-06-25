using assestment.Data;
using assestment.Dto.Login;
using assestment.Interfaces.Auth;
using assestment.Interfaces.Email;
using assestment.Models;
using assestment.Response;
using Microsoft.EntityFrameworkCore;

namespace assestment.Services.Auth;

public class AccesService : IAccesService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    
    public AccesService(AppDbContext dbContext, IEmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    public async Task<SystemResponse<Usuario>> Login(AuthUserDto user)
    {
        try
        {
            var usuario = await _dbContext.Usuarios.FirstOrDefaultAsync(x => x.Email == user.Email);
            if (usuario == null)
            {
                return new SystemResponse<Usuario> { Success = false, Message = "Email not found" };
            }

            bool isValid = BCrypt.Net.BCrypt.Verify(user.Password, usuario.PasswordHash);
            if (!isValid)
            {
                return new SystemResponse<Usuario> { Success = false, Message = "Invalid credentials" };
            }

            return new SystemResponse<Usuario> { Success = true, Data = usuario };
        }
        catch (Exception e)
        {
            return new SystemResponse<Usuario> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<Usuario>> Register(RegisterUserDto user)
    {
        try
        {
            var emailExists = await _dbContext.Usuarios.AnyAsync(x => x.Email == user.Email);
            if (emailExists)
            {
                return new SystemResponse<Usuario> { Success = false, Message = "Email already in use" };
            }

            var documentExists = await _dbContext.Usuarios.AnyAsync(x => x.NumeroDocumento == user.Document);
            if (documentExists)
            {
                return new SystemResponse<Usuario> { Success = false, Message = "Document already registered" };
            }

            var newUser = new Usuario
            {
                Email = user.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
                Nombres = user.Name,
                Apellidos = user.Surnames,
                NumeroDocumento = user.Document,
                FechaNacimiento = user.Birthdate,
                Rol = user.Role.ToString()
            };

            _dbContext.Usuarios.Add(newUser);
            await _dbContext.SaveChangesAsync();
            await _emailService.SendWelcomeEmail(newUser.Email, newUser.Nombres);
            return new SystemResponse<Usuario> { Success = true, Data = newUser };
        }
        catch (Exception e)
        {
            return new SystemResponse<Usuario> { Success = false, Message = e.Message };
        }
    }
}