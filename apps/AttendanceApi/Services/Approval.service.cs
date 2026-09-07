using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Approval;
using AttendanceApi.DTOs.AttendanceAdjustment;
using AttendanceApi.DTOs.LeaveRequest;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AttendanceApi.Services;

public class ApprovalService : IApprovalService
{
    private readonly AttendanceDbContext _context;
    private readonly ILogger<ApprovalService>? _logger;

    public ApprovalService(AttendanceDbContext context, ILogger<ApprovalService>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LeaveRequestResponseDto> ApproveOrRejectLeaveRequestAsync(long id, ApprovalActionDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var isApprove = string.Equals(dto.Action, "APPROVE", StringComparison.OrdinalIgnoreCase);
        var isReject = string.Equals(dto.Action, "REJECT", StringComparison.OrdinalIgnoreCase);

        if (!isApprove && !isReject)
        {
            throw new ArgumentException($"Hành động '{dto.Action}' không hợp lệ. Chỉ chấp nhận 'APPROVE' hoặc 'REJECT'.");
        }

        if (isReject && string.IsNullOrWhiteSpace(dto.RejectionReason))
        {
            throw new ArgumentException("Lý do từ chối không được để trống khi từ chối đơn.");
        }

        var approver = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == dto.ApproverId);

        if (approver == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy người duyệt với ID = {dto.ApproverId}");
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
            throw new InvalidOperationException($"Chỉ có thể phê duyệt hoặc từ chối đơn khi ở trạng thái PENDING. Trạng thái hiện tại: {leaveRequest.Status}");
        }

        // Data Scoping Check: Trưởng phòng chỉ duyệt đơn của nhân viên thuộc cùng phòng ban
        var approverUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId == approver.Id);

        if (approverUser != null && approverUser.Role == UserRole.Manager)
        {
            if (leaveRequest.Employee == null || leaveRequest.Employee.DepartmentId != approver.DepartmentId)
            {
                throw new UnauthorizedAccessException("Trưởng phòng chỉ được phép phê duyệt đơn của nhân viên thuộc phòng ban mình quản lý.");
            }
        }

        var now = DateTime.UtcNow;
        leaveRequest.Status = isApprove ? RequestStatus.Approved : RequestStatus.Rejected;
        leaveRequest.ApproverId = approver.Id;
        leaveRequest.ApprovedAt = now;
        leaveRequest.RejectionReason = isApprove ? null : dto.RejectionReason?.Trim();
        leaveRequest.UpdatedAt = now;

        await _context.SaveChangesAsync();

        _logger?.LogInformation(
            "Người duyệt ID {ApproverId} đã {Action} đơn xin nghỉ phép ID {Id} của nhân viên ID {EmployeeId}.",
            approver.Id,
            isApprove ? "CHẤP THUẬN" : "TỪ CHỐI",
            id,
            leaveRequest.EmployeeId);

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
            ApproverId = approver.Id,
            ApproverFullName = approver.FullName,
            ApprovedAt = leaveRequest.ApprovedAt,
            RejectionReason = leaveRequest.RejectionReason,
            CreatedAt = leaveRequest.CreatedAt
        };
    }

    public async Task<AttendanceAdjustmentResponseDto> ApproveOrRejectAdjustmentAsync(long id, ApprovalActionDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var isApprove = string.Equals(dto.Action, "APPROVE", StringComparison.OrdinalIgnoreCase);
        var isReject = string.Equals(dto.Action, "REJECT", StringComparison.OrdinalIgnoreCase);

        if (!isApprove && !isReject)
        {
            throw new ArgumentException($"Hành động '{dto.Action}' không hợp lệ. Chỉ chấp nhận 'APPROVE' hoặc 'REJECT'.");
        }

        if (isReject && string.IsNullOrWhiteSpace(dto.RejectionReason))
        {
            throw new ArgumentException("Lý do từ chối không được để trống khi từ chối đơn.");
        }

        var approver = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == dto.ApproverId);

        if (approver == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy người duyệt với ID = {dto.ApproverId}");
        }

        var adjustment = await _context.AttendanceAdjustments
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (adjustment == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn giải trình với ID = {id}");
        }

        if (adjustment.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể phê duyệt hoặc từ chối đơn khi ở trạng thái PENDING. Trạng thái hiện tại: {adjustment.Status}");
        }

        // Data Scoping Check: Trưởng phòng chỉ duyệt đơn của nhân viên thuộc cùng phòng ban
        var approverUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId == approver.Id);

        if (approverUser != null && approverUser.Role == UserRole.Manager)
        {
            if (adjustment.Employee == null || adjustment.Employee.DepartmentId != approver.DepartmentId)
            {
                throw new UnauthorizedAccessException("Trưởng phòng chỉ được phép phê duyệt đơn của nhân viên thuộc phòng ban mình quản lý.");
            }
        }

        var now = DateTime.UtcNow;
        adjustment.Status = isApprove ? RequestStatus.Approved : RequestStatus.Rejected;
        adjustment.ApproverId = approver.Id;
        adjustment.ApprovedAt = now;
        adjustment.RejectionReason = isApprove ? null : dto.RejectionReason?.Trim();
        adjustment.UpdatedAt = now;

        await _context.SaveChangesAsync();

        _logger?.LogInformation(
            "Người duyệt ID {ApproverId} đã {Action} đơn giải trình công ID {Id} của nhân viên ID {EmployeeId}.",
            approver.Id,
            isApprove ? "CHẤP THUẬN" : "TỪ CHỐI",
            id,
            adjustment.EmployeeId);

        return new AttendanceAdjustmentResponseDto
        {
            Id = adjustment.Id,
            EmployeeId = adjustment.EmployeeId,
            EmployeeCode = adjustment.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = adjustment.Employee?.FullName ?? string.Empty,
            DepartmentName = adjustment.Employee?.Department?.Name,
            WorkDate = adjustment.WorkDate,
            AdjustmentType = adjustment.AdjustmentType.ToString().ToUpper(),
            AdjustedCheckIn = adjustment.AdjustedCheckIn,
            AdjustedCheckOut = adjustment.AdjustedCheckOut,
            Reason = adjustment.Reason,
            Status = adjustment.Status.ToString().ToUpper(),
            ApproverId = approver.Id,
            ApproverFullName = approver.FullName,
            ApprovedAt = adjustment.ApprovedAt,
            RejectionReason = adjustment.RejectionReason,
            CreatedAt = adjustment.CreatedAt
        };
    }
}