using assestment.Enum;

namespace assestment.Dto.Login;

public class RegisterUserDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public string Surnames { get; set; }
    public string Document { get; set; }
    
    public DateOnly Birthdate { get; set; }
    
    public string Role { get; set; } 
    
}