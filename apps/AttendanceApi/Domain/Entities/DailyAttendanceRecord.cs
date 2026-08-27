using AttendanceApi.Domain.Enums;

namespace AttendanceApi.Domain.Entities;

public class DailyAttendanceRecord
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public int? WorkShiftId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int LateMinutes { get; set; } = 0;
    public int EarlyMinutes { get; set; } = 0;
    public decimal WorkHours { get; set; } = 0.00m;
    public decimal OvertimeHours { get; set; } = 0.00m;
    public DailyAttendanceStatus Status { get; set; } = DailyAttendanceStatus.Absent;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Employee Employee { get; set; } = null!;
    public virtual WorkShift? WorkShift { get; set; }
}
