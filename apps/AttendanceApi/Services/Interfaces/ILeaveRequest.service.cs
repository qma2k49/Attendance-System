using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.LeaveRequest;

namespace AttendanceApi.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequestResponseDto> CreateAsync(CreateLeaveRequestDto dto);
    Task<PagedResultDto<LeaveRequestResponseDto>> GetPagedAsync(LeaveRequestFilterDto filter);
    Task<LeaveRequestResponseDto?> GetByIdAsync(long id);
    Task<LeaveRequestResponseDto> UpdateAsync(long id, UpdateLeaveRequestDto dto);
    Task<bool> CancelAsync(long id);
}