using System.ComponentModel.DataAnnotations;

namespace NZWalks.MVC.Models;

public class RegionFormViewModel
{
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
