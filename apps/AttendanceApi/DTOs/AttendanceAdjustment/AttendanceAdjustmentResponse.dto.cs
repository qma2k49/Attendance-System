namespace AttendanceApi.DTOs.AttendanceAdjustment;

public class AttendanceAdjustmentResponseDto
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public DateOnly WorkDate { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public DateTime? AdjustedCheckIn { get; set; }
    public DateTime? AdjustedCheckOut { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ApproverId { get; set; }
    public string? ApproverFullName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}