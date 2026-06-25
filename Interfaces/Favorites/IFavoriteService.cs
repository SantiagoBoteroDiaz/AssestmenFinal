using assestment.Models;
using assestment.Response;

namespace assestment.Interfaces.Favorite;

public interface IFavoriteService
{
    Task<SystemResponse<Favorito>> AddFavorite(Guid userId, Guid propertyId);
    Task<SystemResponse<bool>> RemoveFavorite(Guid userId, Guid propertyId);
    Task<SystemResponse<ICollection<Inmueble>>> GetFavoritesByUser(Guid userId);
    
}