namespace NZWalks.MVC.ApiClients;

/// <summary>
/// Stable wrapper over the NSwag-generated client. Generated method names change
/// whenever NZWalks.API renames an action, so confine that churn to RegionsApi.
/// </summary>
public interface IRegionsApi
{
    Task<IReadOnlyList<RegionDto>> GetAllAsync(CancellationToken ct = default);

    Task<RegionDto> GetAsync(int id, CancellationToken ct = default);

    Task<RegionDto> CreateAsync(string code, string name, FileParameter? image, CancellationToken ct = default);

    Task<RegionDto> UpdateAsync(int id, string code, string name, FileParameter? image, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
