using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Common;

namespace AttendanceApi.DTOs.AttendanceLogs;

public class RawAttendanceLogFilterDto
{
    public int? DeviceId { get; set; }
    public string? DeviceUserId { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public ProcessedStatus? ProcessedStatus { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
