using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.DailyAttendance;

namespace AttendanceApi.Services;

public interface IDailyAttendanceService
{
    Task<PagedResultDto<DailyAttendanceRecordResponseDto>> GetPagedAsync(
        DailyAttendanceFilterDto filter, 
        CancellationToken cancellationToken = default);

    Task<DailyAttendanceRecordResponseDto?> GetByIdAsync(
        long id, 
        CancellationToken cancellationToken = default);
}