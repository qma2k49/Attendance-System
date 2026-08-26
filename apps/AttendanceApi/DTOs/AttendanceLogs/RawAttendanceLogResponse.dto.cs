namespace AttendanceApi.DTOs.AttendanceLogs;

public class RawAttendanceLogResponseDto
{
    public long Id { get; set; }
    public int DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceUserId { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeFullName { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime CheckTime { get; set; }
    public string VerifyMode { get; set; } = string.Empty;
    public string ProcessedStatus { get; set; } = string.Empty;
    public string? RawPayload { get; set; }
    public DateTime CreatedAt { get; set; }
}