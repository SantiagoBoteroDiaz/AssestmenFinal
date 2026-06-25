using assestment.Data;
using assestment.Interfaces.Favorite;
using assestment.Models;
using assestment.Response;
using Microsoft.EntityFrameworkCore;

namespace assestment.Services.Favorite;

public class FavoriteService : IFavoriteService
{
    private readonly AppDbContext _dbContext;

    public FavoriteService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemResponse<Favorito>> AddFavorite(Guid userId, Guid propertyId)
    {
        try
        {
            var propertyExists = await _dbContext.Inmuebles.AnyAsync(x => x.Id == propertyId);
            if (!propertyExists)
            {
                return new SystemResponse<Favorito> { Success = false, Message = "No property found" };
            }

            var alreadyExists = await _dbContext.Favoritos
                .AnyAsync(x => x.UsuarioId == userId && x.InmuebleId == propertyId);

            if (alreadyExists)
            {
                return new SystemResponse<Favorito> { Success = false, Message = "Property already in favorites" };
            }

            var newFavorite = new Favorito
            {
                UsuarioId = userId,
                InmuebleId = propertyId
            };

            _dbContext.Favoritos.Add(newFavorite);
            await _dbContext.SaveChangesAsync();

            return new SystemResponse<Favorito> { Success = true, Data = newFavorite };
        }
        catch (Exception e)
        {
            return new SystemResponse<Favorito> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<bool>> RemoveFavorite(Guid userId, Guid propertyId)
    {
        try
        {
            var favorite = await _dbContext.Favoritos
                .FirstOrDefaultAsync(x => x.UsuarioId == userId && x.InmuebleId == propertyId);

            if (favorite == null)
            {
                return new SystemResponse<bool> { Success = false, Message = "Favorite not found" };
            }

            _dbContext.Favoritos.Remove(favorite);
            await _dbContext.SaveChangesAsync();

            return new SystemResponse<bool> { Success = true, Data = true };
        }
        catch (Exception e)
        {
            return new SystemResponse<bool> { Success = false, Message = e.Message };
        }
    }

    public async Task<SystemResponse<ICollection<Inmueble>>> GetFavoritesByUser(Guid userId)
    {
        try
        {
            var favorites = await _dbContext.Favoritos
                .Where(x => x.UsuarioId == userId)
                .Select(x => x.Inmueble)
                .ToListAsync();

            return new SystemResponse<ICollection<Inmueble>> { Success = true, Data = favorites };
        }
        catch (Exception e)
        {
            return new SystemResponse<ICollection<Inmueble>> { Success = false, Message = e.Message };
        }
    }
}