using AttendanceApi.Domain.Enums;

namespace AttendanceApi.DTOs.Devices;

public class DeviceFilterDto
{
    public string? Keyword { get; set; }
    public DeviceStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}