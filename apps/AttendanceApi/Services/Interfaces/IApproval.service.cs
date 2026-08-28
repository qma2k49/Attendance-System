using AttendanceApi.DTOs.Approval;
using AttendanceApi.DTOs.AttendanceAdjustment;
using AttendanceApi.DTOs.LeaveRequest;

namespace AttendanceApi.Services;

public interface IApprovalService
{
    Task<LeaveRequestResponseDto> ApproveOrRejectLeaveRequestAsync(long id, ApprovalActionDto dto);
    Task<AttendanceAdjustmentResponseDto> ApproveOrRejectAdjustmentAsync(long id, ApprovalActionDto dto);
}