namespace AttendanceApi.DTOs.MonthlyTimesheet;

public class AggregateTimesheetResultDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalEmployeesProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedLockedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}