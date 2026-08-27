using AttendanceApi.Domain.Enums;

namespace AttendanceApi.Domain.Entities;

public class LeaveRequest
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; } = LeaveType.Annual;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalDays { get; set; } = 1.0m;
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