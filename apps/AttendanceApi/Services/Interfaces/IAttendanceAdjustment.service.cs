using AttendanceApi.DTOs.AttendanceAdjustment;
using AttendanceApi.DTOs.Common;

namespace AttendanceApi.Services;

public interface IAttendanceAdjustmentService
{
    Task<AttendanceAdjustmentResponseDto> CreateAsync(CreateAttendanceAdjustmentDto dto);
    Task<PagedResultDto<AttendanceAdjustmentResponseDto>> GetPagedAsync(AttendanceAdjustmentFilterDto filter);
    Task<AttendanceAdjustmentResponseDto?> GetByIdAsync(long id);
    Task<AttendanceAdjustmentResponseDto> UpdateAsync(long id, UpdateAttendanceAdjustmentDto dto);
    Task<bool> CancelAsync(long id);
}