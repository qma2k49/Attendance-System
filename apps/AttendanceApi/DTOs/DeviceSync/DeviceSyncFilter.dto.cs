using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Common;

namespace AttendanceApi.DTOs.DeviceSync;

public class DeviceSyncFilterDto
{
    public int? DeviceId { get; set; }
    public SyncStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}