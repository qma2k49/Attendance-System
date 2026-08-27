namespace AttendanceApi.DTOs.DailyAttendance;

public class DailyAttendanceRecordResponseDto
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int WorkShiftId { get; set; }
    public string WorkShiftCode { get; set; } = string.Empty;
    public string WorkShiftName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyMinutes { get; set; }
    public decimal WorkHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
}