using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.MonthlyTimesheet;

public class AggregateTimesheetRequestDto
{
    [Required(ErrorMessage = "Year không được để trống")]
    [Range(2000, 2100, ErrorMessage = "Năm phải nằm trong khoảng 2000 đến 2100")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Month không được để trống")]
    [Range(1, 12, ErrorMessage = "Tháng phải nằm trong khoảng 1 đến 12")]
    public int Month { get; set; }

    public int? EmployeeId { get; set; }

    public int? DepartmentId { get; set; }

    [Range(0.0, 31.0, ErrorMessage = "Số ngày công chuẩn phải từ 0 đến 31 ngày")]
    public decimal StandardWorkingDays { get; set; } = 22.0m;
}