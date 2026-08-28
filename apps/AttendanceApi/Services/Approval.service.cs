using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Approval;
using AttendanceApi.DTOs.AttendanceAdjustment;
using AttendanceApi.DTOs.LeaveRequest;
using AttendanceApi.Hubs;
using AttendanceApi.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

public class ApprovalService : IApprovalService
{
    private readonly AttendanceDbContext _context;
    private readonly IHubContext<AttendanceHub>? _hubContext;

    public ApprovalService(AttendanceDbContext context, IHubContext<AttendanceHub>? hubContext = null)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<LeaveRequestResponseDto> ApproveOrRejectLeaveRequestAsync(long id, ApprovalActionDto dto)
    {
        var action = dto.Action.Trim().ToUpper();
        if (action != "APPROVE" && action != "REJECT")
        {
            throw new ArgumentException("Hành động không hợp lệ. Chỉ chấp nhận 'APPROVE' hoặc 'REJECT'.");
        }

        if (action == "REJECT" && string.IsNullOrWhiteSpace(dto.RejectionReason))
        {
            throw new ArgumentException("Bắt buộc phải nhập lý do từ chối (RejectionReason) khi REJECT đơn.");
        }

        var approver = await _context.Employees.FindAsync(dto.ApproverId);
        if (approver == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy người duyệt (Approver) với ID = {dto.ApproverId}");
        }

        var leaveRequest = await _context.LeaveRequests
            .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (leaveRequest == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn xin nghỉ phép với ID = {id}");
        }

        if (leaveRequest.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể duyệt/từ chối đơn khi ở trạng thái PENDING. Trạng thái hiện tại: {leaveRequest.Status}");
        }

        var now = DateTime.UtcNow;
        leaveRequest.ApproverId = approver.Id;
        leaveRequest.ApprovedAt = now;
        leaveRequest.UpdatedAt = now;

        if (action == "APPROVE")
        {
            leaveRequest.Status = RequestStatus.Approved;
            leaveRequest.RejectionReason = null;
        }
        else
        {
            leaveRequest.Status = RequestStatus.Rejected;
            leaveRequest.RejectionReason = dto.RejectionReason?.Trim();
        }

        await _context.SaveChangesAsync();

        // Bắn thông báo thời gian thực qua SignalR Hub
        if (_hubContext != null)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveRequestStatusChanged", new
            {
                requestType = "LEAVE_REQUEST",
                requestId = leaveRequest.Id,
                employeeId = leaveRequest.EmployeeId,
                status = leaveRequest.Status.ToString().ToUpper(),
                approverName = approver.FullName,
                approvedAt = leaveRequest.ApprovedAt,
                rejectionReason = leaveRequest.RejectionReason
            });
        }

        return new LeaveRequestResponseDto
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            EmployeeCode = leaveRequest.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = leaveRequest.Employee?.FullName ?? string.Empty,
            DepartmentName = leaveRequest.Employee?.Department?.Name,
            LeaveType = leaveRequest.LeaveType.ToString().ToUpper(),
            FromDate = leaveRequest.FromDate,
            ToDate = leaveRequest.ToDate,
            TotalDays = leaveRequest.TotalDays,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status.ToString().ToUpper(),
            ApproverId = leaveRequest.ApproverId,
            ApproverFullName = approver.FullName,
            ApprovedAt = leaveRequest.ApprovedAt,
            RejectionReason = leaveRequest.RejectionReason,
            CreatedAt = leaveRequest.CreatedAt
        };
    }

    public async Task<AttendanceAdjustmentResponseDto> ApproveOrRejectAdjustmentAsync(long id, ApprovalActionDto dto)
    {
        var action = dto.Action.Trim().ToUpper();
        if (action != "APPROVE" && action != "REJECT")
        {
            throw new ArgumentException("Hành động không hợp lệ. Chỉ chấp nhận 'APPROVE' hoặc 'REJECT'.");
        }

        if (action == "REJECT" && string.IsNullOrWhiteSpace(dto.RejectionReason))
        {
            throw new ArgumentException("Bắt buộc phải nhập lý do từ chối (RejectionReason) khi REJECT đơn.");
        }

        var approver = await _context.Employees.FindAsync(dto.ApproverId);
        if (approver == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy người duyệt (Approver) với ID = {dto.ApproverId}");
        }

        var adjustment = await _context.AttendanceAdjustments
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (adjustment == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn giải trình chấm công với ID = {id}");
        }

        if (adjustment.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể duyệt/từ chối đơn khi ở trạng thái PENDING. Trạng thái hiện tại: {adjustment.Status}");
        }

        var now = DateTime.UtcNow;
        adjustment.ApproverId = approver.Id;
        adjustment.ApprovedAt = now;
        adjustment.UpdatedAt = now;

        if (action == "APPROVE")
        {
            adjustment.Status = RequestStatus.Approved;
            adjustment.RejectionReason = null;
        }
        else
        {
            adjustment.Status = RequestStatus.Rejected;
            adjustment.RejectionReason = dto.RejectionReason?.Trim();
        }

        await _context.SaveChangesAsync();

        // Bắn thông báo thời gian thực qua SignalR Hub
        if (_hubContext != null)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveRequestStatusChanged", new
            {
                requestType = "ATTENDANCE_ADJUSTMENT",
                requestId = adjustment.Id,
                employeeId = adjustment.EmployeeId,
                workDate = adjustment.WorkDate,
                status = adjustment.Status.ToString().ToUpper(),
                approverName = approver.FullName,
                approvedAt = adjustment.ApprovedAt,
                rejectionReason = adjustment.RejectionReason
            });
        }

        return new AttendanceAdjustmentResponseDto
        {
            Id = adjustment.Id,
            EmployeeId = adjustment.EmployeeId,
            EmployeeCode = adjustment.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = adjustment.Employee?.FullName ?? string.Empty,
            DepartmentName = adjustment.Employee?.Department?.Name,
            WorkDate = adjustment.WorkDate,
            AdjustmentType = adjustment.AdjustmentType == AdjustmentType.ForgottenCheckIn ? "FORGOTTEN_CHECKIN" :
                             adjustment.AdjustmentType == AdjustmentType.ForgottenCheckOut ? "FORGOTTEN_CHECKOUT" :
                             adjustment.AdjustmentType == AdjustmentType.BusinessTrip ? "BUSINESS_TRIP" : "OVERTIME_CLAIM",
            AdjustedCheckIn = adjustment.AdjustedCheckIn,
            AdjustedCheckOut = adjustment.AdjustedCheckOut,
            Reason = adjustment.Reason,
            Status = adjustment.Status.ToString().ToUpper(),
            ApproverId = adjustment.ApproverId,
            ApproverFullName = approver.FullName,
            ApprovedAt = adjustment.ApprovedAt,
            RejectionReason = adjustment.RejectionReason,
            CreatedAt = adjustment.CreatedAt
        };
    }
}