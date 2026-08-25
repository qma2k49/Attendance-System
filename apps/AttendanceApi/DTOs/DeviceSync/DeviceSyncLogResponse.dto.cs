namespace AttendanceApi.DTOs.DeviceSync;

public class DeviceSyncLogResponseDto
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string SyncType { get; set; } = string.Empty;
    public int RecordsPulled { get; set; }
    public int RecordsInserted { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime SyncedAt { get; set; }
}