using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.Approval;

public class ApprovalActionDto
{
    [Required(ErrorMessage = "ApproverId không được để trống")]
    public int ApproverId { get; set; }

    [Required(ErrorMessage = "Action không được để trống")]
    public string Action { get; set; } = string.Empty; // APPROVE hoặc REJECT

    [MaxLength(1000, ErrorMessage = "Lý do từ chối không vượt quá 1000 ký tự")]
    public string? RejectionReason { get; set; }
}