using System.ComponentModel.DataAnnotations;

namespace NZWalks.MVC.Models;

public class RegionFormViewModel
{
    // This length rule is a client-side convention matching the NZWalks domain's
    // real region codes (AKL, BOP, WGN, ...), not a mirrored server constraint:
    // the API has no length check at any layer (AddRegionRequestDto carries no
    // annotations and the Code column is nvarchar(max)), so the API would accept
    // a Code of any length. The API is simply never asked to, because this form
    // is the only path that reaches it.
    [Required]
    [StringLength(3, MinimumLength = 2, ErrorMessage = "Code must be 2 or 3 characters.")]
    public string Code { get; set; } = default!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = default!;

    [Display(Name = "Image")]
    public IFormFile? Image { get; set; }

    /// <summary>Populated on Edit so the current image can be previewed.</summary>
    public string? ExistingImageUrl { get; set; }
}
