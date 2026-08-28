namespace AttendanceApi.DTOs.MonthlyTimesheet;

public class MonthlyTimesheetResponseDto
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal StandardWorkingDays { get; set; }
    public decimal ActualWorkingDays { get; set; }
    public decimal ActualWorkingHours { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal AbsentDays { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyMinutes { get; set; }
    public int LateOccurrences { get; set; }
    public int EarlyOccurrences { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal TotalPayableDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? FinalizedBy { get; set; }
    public string? FinalizerFullName { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}