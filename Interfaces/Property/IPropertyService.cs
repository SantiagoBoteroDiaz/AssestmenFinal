using assestment.Dto.Login;
using assestment.Dto.Property;
using assestment.Models;
using assestment.Response;

namespace assestment.Interfaces.Property;

public interface IPropertyService
{
    public Task<SystemResponse<Inmueble>> CreateProperty(PropertyDto property, Guid propertyId); 
    public Task<SystemResponse<ICollection<Inmueble>>> GetAllProperties(); 
    public Task<SystemResponse<Inmueble>> GetPropertyById(Guid id);
    Task<SystemResponse<Inmueble>> DesactiveProperty(Guid id, Guid ownerId);
    
    Task<SystemResponse<ICollection<Inmueble>>> SearchAvailableProperties(string? ubicacion, DateOnly? fechaInicio, DateOnly? fechaFin);
    public Task<SystemResponse<ICollection<Inmueble>>> GetAllPropertyByOwner(Guid ownerId);
    
    Task<SystemResponse<Inmueble>> UpdateProperty(Guid id, PropertyDto property, Guid ownerId);
}