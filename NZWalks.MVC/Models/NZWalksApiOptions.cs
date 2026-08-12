namespace NZWalks.MVC.Models;

public sealed class NZWalksApiOptions
{
    public string BaseUrl { get; set; } = default!;

    /// <summary>
    /// Turns an API-relative path (e.g. "/images/regions/abc.png") into an absolute
    /// URL. NZWalks.API saves uploads into its own wwwroot and returns host-relative
    /// paths, which would 404 against this app's host if rendered verbatim.
    /// </summary>
    public string? ResolveUrl(string? apiRelativePath)
        => string.IsNullOrWhiteSpace(apiRelativePath)
            ? null
            : $"{BaseUrl.TrimEnd('/')}/{apiRelativePath.TrimStart('/')}";
}
