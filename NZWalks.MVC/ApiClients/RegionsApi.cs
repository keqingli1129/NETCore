namespace NZWalks.MVC.ApiClients;

public sealed class RegionsApi : IRegionsApi
{
    private readonly INZWalksApiClient _client;

    public RegionsApi(INZWalksApiClient client) => _client = client;

    public async Task<IReadOnlyList<RegionDto>> GetAllAsync(CancellationToken ct = default)
        => (await _client.RegionsAllAsync(ct)).ToList();

    public Task<RegionDto> GetAsync(int id, CancellationToken ct = default)
        => _client.RegionsGETAsync(id, ct);

    public Task<RegionDto> CreateAsync(string code, string name, FileParameter? image, CancellationToken ct = default)
        => _client.RegionsPOSTAsync(code, name, image!, ct);

    public async Task<RegionDto> UpdateAsync(int id, string code, string name, FileParameter? image, CancellationToken ct = default)
    {
        // Generated RegionsPUTAsync returns Task (the API responds 204/200 with no
        // body), but the facade contract promises the updated RegionDto back to
        // callers. Fetch the resource after the write completes so the contract
        // holds regardless of what NZWalks.API's PUT action returns.
        await _client.RegionsPUTAsync(id, code, name, image!, ct);
        return await _client.RegionsGETAsync(id, ct);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _client.RegionsDELETEAsync(id, ct);
}
