namespace AttendanceApi.DTOs.Devices;

public class DeviceResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public int MappedEmployeeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}