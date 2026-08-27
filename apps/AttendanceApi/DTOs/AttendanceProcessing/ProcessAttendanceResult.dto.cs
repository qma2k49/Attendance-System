namespace AttendanceApi.DTOs.AttendanceProcessing;

public class ProcessAttendanceResultDto
{
    public DateOnly WorkDate { get; set; }
    public int TotalEmployeesProcessed { get; set; }
    public int TotalRawLogsProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
