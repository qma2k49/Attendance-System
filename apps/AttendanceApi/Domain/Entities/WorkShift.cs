namespace AttendanceApi.Domain.Entities;

public class WorkShift
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly? BreakStartTime { get; set; }
    public TimeOnly? BreakEndTime { get; set; }
    public int GracePeriodMinutes { get; set; } = 0;
    public decimal WorkHours { get; set; } = 8.00m;
    public bool IsOvernight { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
    public virtual ICollection<DailyAttendanceRecord> DailyAttendanceRecords { get; set; } = new List<DailyAttendanceRecord>();
}
