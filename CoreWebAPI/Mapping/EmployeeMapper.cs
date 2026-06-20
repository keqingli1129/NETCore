using CoreMVC.Contracts.Employees;
using CoreMVC.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace CoreWebAPI.Mapping;

/// <summary>
/// Compile-time mappings between <see cref="Employee"/> and its contract DTOs.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class EmployeeMapper
{
    /// <summary>SQL-translatable projection used by list/get queries.</summary>
    public static partial IQueryable<EmployeeDto> ProjectToDto(IQueryable<Employee> employees);

    public static partial EmployeeDto ToDto(Employee employee);

    public static partial Employee ToEntity(CreateEmployeeDto dto);

    /// <summary>Copies the editable fields of <paramref name="dto"/> onto an existing entity.</summary>
    public static partial void Update(CreateEmployeeDto dto, Employee employee);
}
