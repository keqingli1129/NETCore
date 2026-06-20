namespace CoreMVC.Contracts.Employees;

public record CreateEmployeeDto
{
    public required string LastName { get; init; }
    public required string FirstName { get; init; }
    public string? Title { get; init; }
    public DateTime? BirthDate { get; init; }
    public DateTime? HireDate { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Region { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? HomePhone { get; init; }
    public int? ReportsTo { get; init; }
}
