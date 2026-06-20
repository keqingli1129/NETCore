using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace PlainNetCoreMVC.Models;

[ModelMetadataType(typeof(CategoryMetadata))]
public partial class Category;

internal class CategoryMetadata
{
    [Required]
    public string CategoryName { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;
}
