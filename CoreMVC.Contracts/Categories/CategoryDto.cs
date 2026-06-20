namespace CoreMVC.Contracts.Categories;

public record CategoryDto
{
    public int CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? Description { get; init; }
}
