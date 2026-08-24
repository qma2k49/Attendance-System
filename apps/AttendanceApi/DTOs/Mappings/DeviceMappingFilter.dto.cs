namespace AttendanceApi.DTOs.Mappings;

public class DeviceMappingFilterDto
{
    public int? DeviceId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Keyword { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}