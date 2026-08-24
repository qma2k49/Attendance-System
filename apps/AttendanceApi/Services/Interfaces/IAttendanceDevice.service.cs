using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.Devices;

namespace AttendanceApi.Services;


public interface IAttendanceDeviceService
{
    Task<PagedResultDto<DeviceResponseDto>> GetPagedAsync(DeviceFilterDto filter);
    Task<DeviceResponseDto?> GetByIdAsync(int id);
    Task<DeviceResponseDto> CreateAsync(CreateDeviceDto dto);
    Task<DeviceResponseDto?> UpdateAsync(int id, UpdateDeviceDto dto);
    Task<bool> DeleteAsync(int id);
}