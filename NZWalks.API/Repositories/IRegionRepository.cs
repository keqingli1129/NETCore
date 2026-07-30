using NZWalks.API.Models;

namespace NZWalks.API.Repositories
{
    public interface IRegionRepository
    {
        Task<IEnumerable<Region>> GetAllAsync();
        Task<Region?> GetAsync(int id);
        Task<Region> AddAsync(Region region);
        Task<Region?> UpdateAsync(int id, Region region);
        Task<Region?> DeleteAsync(int id);

        /// <summary>Walks reference Regions via FK_Walks_Regions, so a region in use cannot be deleted.</summary>
        Task<bool> HasWalksAsync(int id);
    }
}
