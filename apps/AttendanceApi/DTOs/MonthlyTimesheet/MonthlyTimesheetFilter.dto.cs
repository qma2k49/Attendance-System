using AttendanceApi.DTOs.Common;

namespace AttendanceApi.DTOs.MonthlyTimesheet;

public class MonthlyTimesheetFilterDto
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? DepartmentId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Status { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}