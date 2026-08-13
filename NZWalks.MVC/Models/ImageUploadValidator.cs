namespace NZWalks.MVC.Models;

// Server-side counterpart to the `accept="image/*"` hint on the Create/Edit
// views, which is advisory only. NZWalks.API writes uploaded files straight
// into wwwroot/images/regions as {Guid}{Path.GetExtension(image.FileName)}
// and serves that directory via UseStaticFiles(), so without this check a
// caller could upload e.g. an .html/.svg file and have it served back as
// attacker-controlled content from the API origin. Single source of truth
// for both RegionsController.Create and RegionsController.Edit so the rule
// only needs to change in one place.
public static class ImageUploadValidator
{
    private const long MaxLengthBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    public const string ErrorMessage =
        "Only image files (.jpg, .jpeg, .png, .gif, .webp) up to 5 MB are allowed.";

    /// <summary>
    /// A null file, or a zero-length file (how ASP.NET Core model binding
    /// represents "no file chosen" for an &lt;input type="file"&gt; left
    /// empty), is always valid - that is the normal "no picture" case both
    /// Create and Edit rely on. Anything with content must be a recognised
    /// image extension, declare an image/* content type, and be within the
    /// size cap.
    /// </summary>
    public static bool IsValid(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return true;
        }

        if (file.Length > MaxLengthBytes)
        {
            return false;
        }

        if (string.IsNullOrEmpty(file.ContentType)
            || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName);
        return !string.IsNullOrEmpty(extension)
               && AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
