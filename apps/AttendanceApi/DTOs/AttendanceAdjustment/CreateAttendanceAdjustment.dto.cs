using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.AttendanceAdjustment;

public class CreateAttendanceAdjustmentDto
{
    [Required(ErrorMessage = "EmployeeId không được để trống")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "WorkDate không được để trống")]
    public DateOnly WorkDate { get; set; }

    [Required(ErrorMessage = "AdjustmentType không được để trống")]
    public string AdjustmentType { get; set; } = string.Empty;

    public DateTime? AdjustedCheckIn { get; set; }

    public DateTime? AdjustedCheckOut { get; set; }

    [Required(ErrorMessage = "Reason không được để trống")]
    [MaxLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự")]
    public string Reason { get; set; } = string.Empty;
}