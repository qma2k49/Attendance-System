namespace AttendanceApi.DTOs.LeaveRequest;

public class LeaveRequestFilterDto
{
    public int? DepartmentId { get; set; }
    public int? EmployeeId { get; set; }
    public string? LeaveType { get; set; }
    public string? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}