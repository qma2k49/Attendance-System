namespace AttendanceApi.Domain.Entities;

public class WorkSchedule
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int WorkShiftId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Employee Employee { get; set; } = null!;
    public virtual WorkShift WorkShift { get; set; } = null!;
}
