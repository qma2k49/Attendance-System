using AttendanceApi.Domain.Enums;

namespace AttendanceApi.Domain.Entities;

public class MonthlyTimesheetSummary
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    public decimal StandardWorkingDays { get; set; } = 0.0m;
    public decimal ActualWorkingDays { get; set; } = 0.0m;
    public decimal ActualWorkingHours { get; set; } = 0.00m;

    public decimal PaidLeaveDays { get; set; } = 0.0m;
    public decimal UnpaidLeaveDays { get; set; } = 0.0m;
    public decimal AbsentDays { get; set; } = 0.0m;

    public int LateMinutes { get; set; } = 0;
    public int EarlyMinutes { get; set; } = 0;
    public int LateOccurrences { get; set; } = 0;
    public int EarlyOccurrences { get; set; } = 0;

    public decimal OvertimeHours { get; set; } = 0.00m;
    public decimal TotalPayableDays { get; set; } = 0.0m;

    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    public int? FinalizedBy { get; set; }
    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual Employee? Finalizer { get; set; }
}