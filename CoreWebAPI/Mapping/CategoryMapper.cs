using CoreMVC.Contracts.Categories;
using CoreMVC.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace CoreWebAPI.Mapping;

/// <summary>
/// Compile-time mappings between <see cref="Category"/> and its contract DTOs.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class CategoryMapper
{
    /// <summary>SQL-translatable projection used by list/get queries.</summary>
    public static partial IQueryable<CategoryDto> ProjectToDto(IQueryable<Category> categories);

    public static partial CategoryDto ToDto(Category category);

    public static partial Category ToEntity(CreateCategoryDto dto);

    /// <summary>Copies the editable fields of <paramref name="dto"/> onto an existing entity.</summary>
    public static partial void Update(CreateCategoryDto dto, Category category);
}
