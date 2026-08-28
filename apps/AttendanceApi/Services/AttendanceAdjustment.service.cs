using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceAdjustment;
using AttendanceApi.DTOs.Common;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

public class AttendanceAdjustmentService : IAttendanceAdjustmentService
{
    private readonly AttendanceDbContext _context;

    public AttendanceAdjustmentService(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceAdjustmentResponseDto> CreateAsync(CreateAttendanceAdjustmentDto dto)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);

        if (employee == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhân viên với ID = {dto.EmployeeId}");
        }

        if (!TryParseAdjustmentType(dto.AdjustmentType, out var adjustmentTypeEnum))
        {
            throw new ArgumentException($"Loại giải trình '{dto.AdjustmentType}' không hợp lệ.");
        }

        if (dto.AdjustedCheckIn.HasValue && dto.AdjustedCheckOut.HasValue && dto.AdjustedCheckOut <= dto.AdjustedCheckIn)
        {
            throw new ArgumentException("Giờ quẹt ra điều chỉnh (AdjustedCheckOut) phải sau giờ quẹt vào điều chỉnh (AdjustedCheckIn).");
        }

        var checkInUtc = dto.AdjustedCheckIn.HasValue
            ? (dto.AdjustedCheckIn.Value.Kind == DateTimeKind.Utc ? dto.AdjustedCheckIn.Value : dto.AdjustedCheckIn.Value.ToUniversalTime())
            : (DateTime?)null;

        var checkOutUtc = dto.AdjustedCheckOut.HasValue
            ? (dto.AdjustedCheckOut.Value.Kind == DateTimeKind.Utc ? dto.AdjustedCheckOut.Value : dto.AdjustedCheckOut.Value.ToUniversalTime())
            : (DateTime?)null;

        var adjustment = new AttendanceAdjustment
        {
            EmployeeId = dto.EmployeeId,
            WorkDate = dto.WorkDate,
            AdjustmentType = adjustmentTypeEnum,
            AdjustedCheckIn = checkInUtc,
            AdjustedCheckOut = checkOutUtc,
            Reason = dto.Reason.Trim(),
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AttendanceAdjustments.Add(adjustment);
        await _context.SaveChangesAsync();

        return new AttendanceAdjustmentResponseDto
        {
            Id = adjustment.Id,
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = employee.FullName,
            DepartmentName = employee.Department?.Name,
            WorkDate = adjustment.WorkDate,
            AdjustmentType = FormatAdjustmentType(adjustment.AdjustmentType),
            AdjustedCheckIn = adjustment.AdjustedCheckIn,
            AdjustedCheckOut = adjustment.AdjustedCheckOut,
            Reason = adjustment.Reason,
            Status = adjustment.Status.ToString().ToUpper(),
            CreatedAt = adjustment.CreatedAt
        };
    }

    public async Task<PagedResultDto<AttendanceAdjustmentResponseDto>> GetPagedAsync(AttendanceAdjustmentFilterDto filter)
    {
        var query = _context.AttendanceAdjustments
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .Include(a => a.Approver)
            .AsNoTracking()
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(a => a.Employee != null && a.Employee.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == filter.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AdjustmentType) && TryParseAdjustmentType(filter.AdjustmentType, out var parsedType))
        {
            query = query.Where(a => a.AdjustmentType == parsedType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<RequestStatus>(filter.Status, true, out var parsedStatus))
        {
            query = query.Where(a => a.Status == parsedStatus);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.WorkDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(a => a.WorkDate <= filter.ToDate.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AttendanceAdjustmentResponseDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeCode = a.Employee != null ? a.Employee.EmployeeCode : string.Empty,
                EmployeeFullName = a.Employee != null ? a.Employee.FullName : string.Empty,
                DepartmentName = a.Employee != null && a.Employee.Department != null ? a.Employee.Department.Name : null,
                WorkDate = a.WorkDate,
                AdjustmentType = a.AdjustmentType == AdjustmentType.ForgottenCheckIn ? "FORGOTTEN_CHECKIN" :
                                 a.AdjustmentType == AdjustmentType.ForgottenCheckOut ? "FORGOTTEN_CHECKOUT" :
                                 a.AdjustmentType == AdjustmentType.BusinessTrip ? "BUSINESS_TRIP" : "OVERTIME_CLAIM",
                AdjustedCheckIn = a.AdjustedCheckIn,
                AdjustedCheckOut = a.AdjustedCheckOut,
                Reason = a.Reason,
                Status = a.Status.ToString().ToUpper(),
                ApproverId = a.ApproverId,
                ApproverFullName = a.Approver != null ? a.Approver.FullName : null,
                ApprovedAt = a.ApprovedAt,
                RejectionReason = a.RejectionReason,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<AttendanceAdjustmentResponseDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<AttendanceAdjustmentResponseDto?> GetByIdAsync(long id)
    {
        var item = await _context.AttendanceAdjustments
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .Include(a => a.Approver)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (item == null) return null;

        return new AttendanceAdjustmentResponseDto
        {
            Id = item.Id,
            EmployeeId = item.EmployeeId,
            EmployeeCode = item.Employee != null ? item.Employee.EmployeeCode : string.Empty,
            EmployeeFullName = item.Employee != null ? item.Employee.FullName : string.Empty,
            DepartmentName = item.Employee != null && item.Employee.Department != null ? item.Employee.Department.Name : null,
            WorkDate = item.WorkDate,
            AdjustmentType = FormatAdjustmentType(item.AdjustmentType),
            AdjustedCheckIn = item.AdjustedCheckIn,
            AdjustedCheckOut = item.AdjustedCheckOut,
            Reason = item.Reason,
            Status = item.Status.ToString().ToUpper(),
            ApproverId = item.ApproverId,
            ApproverFullName = item.Approver != null ? item.Approver.FullName : null,
            ApprovedAt = item.ApprovedAt,
            RejectionReason = item.RejectionReason,
            CreatedAt = item.CreatedAt
        };
    }

    public async Task<AttendanceAdjustmentResponseDto> UpdateAsync(long id, UpdateAttendanceAdjustmentDto dto)
    {
        if (!TryParseAdjustmentType(dto.AdjustmentType, out var adjustmentTypeEnum))
        {
            throw new ArgumentException($"Loại giải trình '{dto.AdjustmentType}' không hợp lệ.");
        }

        if (dto.AdjustedCheckIn.HasValue && dto.AdjustedCheckOut.HasValue && dto.AdjustedCheckOut <= dto.AdjustedCheckIn)
        {
            throw new ArgumentException("Giờ quẹt ra điều chỉnh (AdjustedCheckOut) phải sau giờ quẹt vào điều chỉnh (AdjustedCheckIn).");
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
            throw new InvalidOperationException($"Chỉ có thể chỉnh sửa đơn giải trình khi ở trạng thái PENDING. Trạng thái hiện tại: {adjustment.Status}");
        }

        var checkInUtc = dto.AdjustedCheckIn.HasValue
            ? (dto.AdjustedCheckIn.Value.Kind == DateTimeKind.Utc ? dto.AdjustedCheckIn.Value : dto.AdjustedCheckIn.Value.ToUniversalTime())
            : (DateTime?)null;

        var checkOutUtc = dto.AdjustedCheckOut.HasValue
            ? (dto.AdjustedCheckOut.Value.Kind == DateTimeKind.Utc ? dto.AdjustedCheckOut.Value : dto.AdjustedCheckOut.Value.ToUniversalTime())
            : (DateTime?)null;

        adjustment.AdjustmentType = adjustmentTypeEnum;
        adjustment.AdjustedCheckIn = checkInUtc;
        adjustment.AdjustedCheckOut = checkOutUtc;
        adjustment.Reason = dto.Reason.Trim();
        adjustment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new AttendanceAdjustmentResponseDto
        {
            Id = adjustment.Id,
            EmployeeId = adjustment.EmployeeId,
            EmployeeCode = adjustment.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = adjustment.Employee?.FullName ?? string.Empty,
            DepartmentName = adjustment.Employee?.Department?.Name,
            WorkDate = adjustment.WorkDate,
            AdjustmentType = FormatAdjustmentType(adjustment.AdjustmentType),
            AdjustedCheckIn = adjustment.AdjustedCheckIn,
            AdjustedCheckOut = adjustment.AdjustedCheckOut,
            Reason = adjustment.Reason,
            Status = adjustment.Status.ToString().ToUpper(),
            CreatedAt = adjustment.CreatedAt
        };
    }

    public async Task<bool> CancelAsync(long id)
    {
        var adjustment = await _context.AttendanceAdjustments.FindAsync(id);
        if (adjustment == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn giải trình với ID = {id}");
        }

        if (adjustment.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể hủy đơn khi ở trạng thái PENDING. Trạng thái hiện tại: {adjustment.Status}");
        }

        adjustment.Status = RequestStatus.Cancelled;
        adjustment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private static bool TryParseAdjustmentType(string input, out AdjustmentType result)
    {
        var normalized = input.Trim().Replace("_", string.Empty);
        return Enum.TryParse(normalized, true, out result);
    }

    private static string FormatAdjustmentType(AdjustmentType type) => type switch
    {
        AdjustmentType.ForgottenCheckIn => "FORGOTTEN_CHECKIN",
        AdjustmentType.ForgottenCheckOut => "FORGOTTEN_CHECKOUT",
        AdjustmentType.BusinessTrip => "BUSINESS_TRIP",
        AdjustmentType.OvertimeClaim => "OVERTIME_CLAIM",
        _ => type.ToString().ToUpper()
    };
}