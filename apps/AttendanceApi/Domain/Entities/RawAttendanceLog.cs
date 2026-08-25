using AttendanceApi.Domain.Enums;

namespace AttendanceApi.Domain.Entities;

public class RawAttendanceLog
{
    public long Id { get; set; }
    public int DeviceId { get; set; }
    public string DeviceUserId { get; set; } = string.Empty;
    public DateTime CheckTime { get; set; }
    public VerifyModeEnum VerifyMode { get; set; } = VerifyModeEnum.Fingerprint;
    public ProcessedStatus ProcessedStatus { get; set; } = ProcessedStatus.Pending;
    public string? RawPayload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    public virtual AttendanceDevice? AttendanceDevice { get; set; }

}