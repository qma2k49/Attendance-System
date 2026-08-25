using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.DeviceSync;

namespace AttendanceApi.Services;


public interface IDeviceSyncService
{
    Task<DeviceSyncLogResponseDto> SyncDeviceAsync(int deviceId, string syncType = "MANUAL_TRIGGER");
    Task<IEnumerable<DeviceSyncLogResponseDto>> SyncAllActiveDevicesAsync(string syncType = "AUTO_SCHEDULED");
    Task<PagedResultDto<DeviceSyncLogResponseDto>> GetLogsPagedAsync(DeviceSyncFilterDto filter);
}