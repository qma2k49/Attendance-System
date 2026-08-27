using AttendanceApi.Domain.Enums;

namespace AttendanceApi.Domain.Entities;

public class AttendanceAdjustment
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public AdjustmentType AdjustmentType { get; set; } = AdjustmentType.ForgottenCheckIn;
    public DateTime? AdjustedCheckIn { get; set; }
    public DateTime? AdjustedCheckOut { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public int? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual Employee? Approver { get; set; }
}