using AttendanceApi.DTOs.AttendanceLogs;
using AttendanceApi.DTOs.Common;

namespace AttendanceApi.Services;


public interface IRawAttendanceLogService
{
    Task<PagedResultDto<RawAttendanceLogResponseDto>> GetPagedAsync(RawAttendanceLogFilterDto filter);
    Task<RawAttendanceLogResponseDto?> GetByIdAsync(long id);
}