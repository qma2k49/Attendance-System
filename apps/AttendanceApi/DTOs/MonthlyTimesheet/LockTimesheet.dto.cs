using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.MonthlyTimesheet;

public class LockTimesheetDto
{
    [Required(ErrorMessage = "Year không được để trống")]
    [Range(2000, 2100, ErrorMessage = "Năm phải nằm trong khoảng 2000 đến 2100")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Month không được để trống")]
    [Range(1, 12, ErrorMessage = "Tháng phải nằm trong khoảng 1 đến 12")]
    public int Month { get; set; }

    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "FinalizerId không được để trống")]
    public int FinalizerId { get; set; }

    [Required(ErrorMessage = "Action không được để trống")]
    public string Action { get; set; } = string.Empty; // FINALIZE hoặc LOCK
}