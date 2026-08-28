using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.LeaveRequest;

public class UpdateLeaveRequestDto
{
    [Required(ErrorMessage = "LeaveType không được để trống")]
    public string LeaveType { get; set; } = string.Empty;

    [Required(ErrorMessage = "FromDate không được để trống")]
    public DateOnly FromDate { get; set; }

    [Required(ErrorMessage = "ToDate không được để trống")]
    public DateOnly ToDate { get; set; }

    [Required(ErrorMessage = "Reason không được để trống")]
    [MaxLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự")]
    public string Reason { get; set; } = string.Empty;
}