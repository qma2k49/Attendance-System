using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.Mappings;

namespace AttendanceApi.Services;


public interface IDeviceEmployeeMappingService
{
    Task<PagedResultDto<DeviceMappingResponseDto>> GetPagedAsync(DeviceMappingFilterDto filter);
    Task<DeviceMappingResponseDto?> GetByIdAsync(int id);
    Task<DeviceMappingResponseDto> CreateAsync(CreateDeviceMappingDto dto);
    Task<bool> DeleteAsync(int id);
}