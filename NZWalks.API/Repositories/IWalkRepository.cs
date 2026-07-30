using NZWalks.API.Models;

namespace NZWalks.API.Repositories
{
    public interface IWalkRepository
    {
        Task<IEnumerable<Walk>> GetAllAsync();
        Task<Walk?> GetAsync(int id);
        Task<Walk> AddAsync(Walk walk);
        Task<Walk?> UpdateAsync(int id, Walk walk);
        Task<Walk?> DeleteAsync(int id);
    }
}
