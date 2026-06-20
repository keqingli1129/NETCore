namespace CoreMVC.Contracts.Categories;

public record CreateCategoryDto
{
    public required string CategoryName { get; init; }
    public string? Description { get; init; }
}
