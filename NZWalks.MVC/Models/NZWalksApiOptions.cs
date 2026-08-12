namespace NZWalks.MVC.Models;

public sealed class NZWalksApiOptions
{
    public string BaseUrl { get; set; } = default!;

    /// <summary>
    /// Turns an API-relative path (e.g. "/images/regions/abc.png") into an absolute
    /// URL. NZWalks.API saves uploads into its own wwwroot and returns host-relative
    /// paths, which would 404 against this app's host if rendered verbatim.
    /// Already-absolute URLs (e.g. seeded rows pointing at external hosts) pass through
    /// untouched instead of getting the base URL prepended.
    /// </summary>
    public string? ResolveUrl(string? apiRelativePath)
    {
        if (string.IsNullOrWhiteSpace(apiRelativePath))
        {
            return null;
        }

        // Seeded regions hold absolute URLs (https://example.com/...); uploads return
        // host-relative paths like /images/regions/x.png. Prepending the base URL to an
        // absolute URL yields "https://localhost:7223/https://example.com/..." and a
        // broken image.
        if (Uri.TryCreate(apiRelativePath, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return apiRelativePath;
        }

        return $"{BaseUrl.TrimEnd('/')}/{apiRelativePath.TrimStart('/')}";
    }
}
