using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.DailyAttendance;

public class DailyAttendanceFilterDto
{
    public int? DepartmentId { get; set; }
    public int? EmployeeId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber phải lớn hơn hoặc bằng 1.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize phải nằm trong khoảng từ 1 đến 100.")]
    public int PageSize { get; set; } = 10;
}