namespace CoreMVC.Contracts.Employees;

public record EmployeeDto
{
    public int EmployeeId { get; init; }
    public string? LastName { get; init; }
    public string? FirstName { get; init; }
    public string? Title { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public DateTime? HireDate { get; init; }
    public int? ReportsTo { get; init; }
}
