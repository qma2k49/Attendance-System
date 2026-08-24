namespace AttendanceApi.DTOs.Mappings;

public class DeviceMappingResponseDto
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceUserId { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}