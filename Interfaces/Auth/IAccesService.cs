using assestment.Dto.Login;
using assestment.Models;
using assestment.Response;

namespace assestment.Interfaces.Auth;

public interface IAccesService
{
    public Task<SystemResponse<Usuario>> Login(AuthUserDto user);  
    public Task<SystemResponse<Usuario>> Register(RegisterUserDto user);
}