using NZWalks.API.Models;

namespace NZWalks.API.Repositories
{
    public interface IDifficultyRepository
    {
        Task<IEnumerable<Difficulty>> GetAllAsync();
        Task<Difficulty?> GetAsync(int id);
        Task<Difficulty> AddAsync(Difficulty difficulty);
        Task<Difficulty?> UpdateAsync(int id, Difficulty difficulty);
        Task<Difficulty?> DeleteAsync(int id);

        /// <summary>Walks reference Difficulties via FK_Walks_Difficulty, so one in use cannot be deleted.</summary>
        Task<bool> HasWalksAsync(int id);
    }
}
