namespace AttendanceApi.DTOs.AttendanceLogs;

public class IngestResponseDto
{
    public int TotalReceived { get; set; }
    public int TotalInserted { get; set; }
    public int TotalSkipped { get; set; }
    public string Message { get; set; } = string.Empty;
}