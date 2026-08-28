using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.LeaveRequest;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly AttendanceDbContext _context;

    public LeaveRequestService(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequestResponseDto> CreateAsync(CreateLeaveRequestDto dto)
    {
        if (dto.ToDate < dto.FromDate)
        {
            throw new ArgumentException("Ngày kết thúc nghỉ (ToDate) phải lớn hơn hoặc bằng ngày bắt đầu (FromDate).");
        }

        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);

        if (employee == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhân viên với ID = {dto.EmployeeId}");
        }

        if (!Enum.TryParse<LeaveType>(dto.LeaveType, true, out var leaveTypeEnum))
        {
            throw new ArgumentException($"Loại nghỉ phép '{dto.LeaveType}' không hợp lệ.");
        }

        // Tính tổng số ngày nghỉ (bao gồm cả ngày bắt đầu và kết thúc)
        var totalDays = (decimal)(dto.ToDate.DayNumber - dto.FromDate.DayNumber + 1);

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = dto.EmployeeId,
            LeaveType = leaveTypeEnum,
            FromDate = dto.FromDate,
            ToDate = dto.ToDate,
            TotalDays = totalDays,
            Reason = dto.Reason.Trim(),
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        return new LeaveRequestResponseDto
        {
            Id = leaveRequest.Id,
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = employee.FullName,
            DepartmentName = employee.Department?.Name,
            LeaveType = leaveRequest.LeaveType.ToString().ToUpper(),
            FromDate = leaveRequest.FromDate,
            ToDate = leaveRequest.ToDate,
            TotalDays = leaveRequest.TotalDays,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status.ToString().ToUpper(),
            CreatedAt = leaveRequest.CreatedAt
        };
    }

    public async Task<PagedResultDto<LeaveRequestResponseDto>> GetPagedAsync(LeaveRequestFilterDto filter)
    {
        var query = _context.LeaveRequests
            .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
            .Include(l => l.Approver)
            .AsNoTracking()
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(l => l.Employee != null && l.Employee.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(l => l.EmployeeId == filter.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.LeaveType) && Enum.TryParse<LeaveType>(filter.LeaveType, true, out var parsedLeaveType))
        {
            query = query.Where(l => l.LeaveType == parsedLeaveType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<RequestStatus>(filter.Status, true, out var parsedStatus))
        {
            query = query.Where(l => l.Status == parsedStatus);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(l => l.ToDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(l => l.FromDate <= filter.ToDate.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new LeaveRequestResponseDto
            {
                Id = l.Id,
                EmployeeId = l.EmployeeId,
                EmployeeCode = l.Employee != null ? l.Employee.EmployeeCode : string.Empty,
                EmployeeFullName = l.Employee != null ? l.Employee.FullName : string.Empty,
                DepartmentName = l.Employee != null && l.Employee.Department != null ? l.Employee.Department.Name : null,
                LeaveType = l.LeaveType.ToString().ToUpper(),
                FromDate = l.FromDate,
                ToDate = l.ToDate,
                TotalDays = l.TotalDays,
                Reason = l.Reason,
                Status = l.Status.ToString().ToUpper(),
                ApproverId = l.ApproverId,
                ApproverFullName = l.Approver != null ? l.Approver.FullName : null,
                ApprovedAt = l.ApprovedAt,
                RejectionReason = l.RejectionReason,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<LeaveRequestResponseDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<LeaveRequestResponseDto?> GetByIdAsync(long id)
    {
        var item = await _context.LeaveRequests
            .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
            .Include(l => l.Approver)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (item == null) return null;

        return new LeaveRequestResponseDto
        {
            Id = item.Id,
            EmployeeId = item.EmployeeId,
            EmployeeCode = item.Employee != null ? item.Employee.EmployeeCode : string.Empty,
            EmployeeFullName = item.Employee != null ? item.Employee.FullName : string.Empty,
            DepartmentName = item.Employee != null && item.Employee.Department != null ? item.Employee.Department.Name : null,
            LeaveType = item.LeaveType.ToString().ToUpper(),
            FromDate = item.FromDate,
            ToDate = item.ToDate,
            TotalDays = item.TotalDays,
            Reason = item.Reason,
            Status = item.Status.ToString().ToUpper(),
            ApproverId = item.ApproverId,
            ApproverFullName = item.Approver != null ? item.Approver.FullName : null,
            ApprovedAt = item.ApprovedAt,
            RejectionReason = item.RejectionReason,
            CreatedAt = item.CreatedAt
        };
    }

    public async Task<LeaveRequestResponseDto> UpdateAsync(long id, UpdateLeaveRequestDto dto)
    {
        if (dto.ToDate < dto.FromDate)
        {
            throw new ArgumentException("Ngày kết thúc nghỉ (ToDate) phải lớn hơn hoặc bằng ngày bắt đầu (FromDate).");
        }

        if (!Enum.TryParse<LeaveType>(dto.LeaveType, true, out var leaveTypeEnum))
        {
            throw new ArgumentException($"Loại nghỉ phép '{dto.LeaveType}' không hợp lệ.");
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
            throw new InvalidOperationException($"Chỉ có thể chỉnh sửa đơn nghỉ phép khi ở trạng thái PENDING. Trạng thái hiện tại: {leaveRequest.Status}");
        }

        leaveRequest.LeaveType = leaveTypeEnum;
        leaveRequest.FromDate = dto.FromDate;
        leaveRequest.ToDate = dto.ToDate;
        leaveRequest.TotalDays = (decimal)(dto.ToDate.DayNumber - dto.FromDate.DayNumber + 1);
        leaveRequest.Reason = dto.Reason.Trim();
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

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
            CreatedAt = leaveRequest.CreatedAt
        };
    }

    public async Task<bool> CancelAsync(long id)
    {
        var leaveRequest = await _context.LeaveRequests.FindAsync(id);
        if (leaveRequest == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn xin nghỉ phép với ID = {id}");
        }

        if (leaveRequest.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể hủy đơn khi ở trạng thái PENDING. Trạng thái hiện tại: {leaveRequest.Status}");
        }

        leaveRequest.Status = RequestStatus.Cancelled;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}