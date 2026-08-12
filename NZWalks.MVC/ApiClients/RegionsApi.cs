using Newtonsoft.Json;

namespace NZWalks.MVC.ApiClients;

public sealed class RegionsApi : IRegionsApi
{
    private readonly INZWalksApiClient _client;

    public RegionsApi(INZWalksApiClient client) => _client = client;

    public async Task<IReadOnlyList<RegionDto>> GetAllAsync(CancellationToken ct = default)
        => (await _client.RegionsAllAsync(ct)).ToList();

    public Task<RegionDto> GetAsync(int id, CancellationToken ct = default)
        => _client.RegionsGETAsync(id, ct);

    public async Task<RegionDto> CreateAsync(string code, string name, FileParameter? image, CancellationToken ct = default)
    {
        try
        {
            return await _client.RegionsPOSTAsync(code, name, RequireFileParameter(image), ct);
        }
        catch (ApiException ex) when (ex.StatusCode == 201)
        {
            // Self-discovered while verifying the null-image fix below (it reproduces
            // for every create, image or not - not specific to a null image).
            // NZWalks.API's PostRegion actually succeeds via
            // CreatedAtAction(...) (HTTP 201), but the committed OpenAPI contract
            // (NZWalks.MVC/OpenAPIs/nzwalks.v1.json, POST /api/Regions) declares only
            // a 200 response for this operation. The generated client only treats
            // status 200 as success and throws ApiException for the real 201, even
            // though the region was created and the response body is the created
            // RegionDto. Recover it here rather than surfacing a false failure.
            var created = JsonConvert.DeserializeObject<RegionDto>(ex.Response);
            return created ?? throw new ApiException(
                "Region was created (201) but the response body could not be parsed as RegionDto.",
                ex.StatusCode, ex.Response, ex.Headers, ex);
        }
    }

    public async Task<RegionDto> UpdateAsync(int id, string code, string name, FileParameter? image, CancellationToken ct = default)
    {
        // Generated RegionsPUTAsync returns Task, not Task<RegionDto>. This is not
        // because the API sends no body back: NZWalks.API's PutRegion actually
        // returns Ok(_mapper.Map<RegionDto>(updatedRegion)). It's because the OpenAPI
        // document declares no response *content schema* for PUT ("200": no
        // "content"), a side effect of the action's declared return type being the
        // untyped IActionResult rather than ActionResult<RegionDto>. NSwag only
        // generates a typed return when the schema says so, so it generates Task
        // here. Fetch the resource after the write completes so the facade's
        // Task<RegionDto> contract holds regardless.
        await _client.RegionsPUTAsync(id, code, name, RequireFileParameter(image), ct);
        return await _client.RegionsGETAsync(id, ct);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _client.RegionsDELETEAsync(id, ct);

    // The generated Regions*Async methods (NZWalks.MVC/obj/nzwalks.v1Client.cs)
    // unconditionally reject a null image:
    //     if (image == null)
    //         throw new System.ArgumentNullException("image");
    // but IRegionsApi must accept image: null for the common "no file chosen"
    // case (create/edit without a picture). Substitute a zero-length FileParameter
    // instead of null: NZWalks.API's RegionsController.SaveImageAsync already
    // treats "image is null || image.Length == 0" identically, returning null and
    // leaving RegionImageUrl unset either way, so an empty upload and no upload
    // are indistinguishable to the server. The file name must be a non-empty,
    // non-whitespace string - MultipartFormDataContent.Add throws ArgumentException
    // otherwise (discovered by running the live check below) - so a placeholder
    // name is used; it is never persisted because Length == 0 short-circuits
    // SaveImageAsync before the name is read. Verified against the live API - see
    // task-2-report.md, "Fix round 1" section.
    private static FileParameter RequireFileParameter(FileParameter? image)
        => image ?? new FileParameter(System.IO.Stream.Null, "no-image", string.Empty);
}
