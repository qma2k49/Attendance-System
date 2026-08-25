using AttendanceApi.Domain.Enums;

namespace AttendanceApi.Domain.Entities;

public class AttendanceDevice
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 4370;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Location { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Online;
    public DateTime? LastSyncAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 1 Device -> N DeviceEmployeeMappings
    public ICollection<DeviceEmployeeMapping> DeviceEmployeeMappings { get; set; } = new List<DeviceEmployeeMapping>();
    public virtual ICollection<RawAttendanceLog> RawAttendanceLogs { get; set; } = new List<RawAttendanceLog>();
    public virtual ICollection<DeviceSyncLog> DeviceSyncLogs { get; set; } = new List<DeviceSyncLog>();
}