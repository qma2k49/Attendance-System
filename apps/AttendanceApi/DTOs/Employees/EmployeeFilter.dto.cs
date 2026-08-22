using AttendanceApi.Domain.Enums;

namespace AttendanceApi.DTOs.Employees;

public class EmployeeFilterDto
{
    public string? Keyword { get; set; }
    public int? DepartmentId { get; set; }
    public EmployeeStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}