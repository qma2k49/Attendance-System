using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.DailyAttendance;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

public class DailyAttendanceService : IDailyAttendanceService
{
    private readonly AttendanceDbContext _context;

    public DailyAttendanceService(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<DailyAttendanceRecordResponseDto>> GetPagedAsync(
        DailyAttendanceFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DailyAttendanceRecords
            .AsNoTracking()
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Include(r => r.WorkShift)
            .AsQueryable();

        // 1. Áp dụng các bộ lọc
        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(r => r.Employee != null && r.Employee.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(r => r.WorkDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(r => r.WorkDate <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<DailyAttendanceStatus>(filter.Status, true, out var statusEnum))
        {
            query = query.Where(r => r.Status == statusEnum);
        }

        // 2. Đếm tổng số lượng bản ghi thỏa mãn điều kiện
        var totalCount = await query.CountAsync(cancellationToken);

        // 3. Sắp xếp giảm dần theo ngày làm việc và phân trang
        var items = await query
            .OrderByDescending(r => r.WorkDate)
            .ThenBy(r => r.EmployeeId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new DailyAttendanceRecordResponseDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeCode = r.Employee != null ? r.Employee.EmployeeCode : string.Empty,
                EmployeeFullName = r.Employee != null ? r.Employee.FullName : string.Empty,
                DepartmentName = r.Employee != null && r.Employee.Department != null ? r.Employee.Department.Name : string.Empty,
                WorkShiftId = r.WorkShiftId ?? 0,
                WorkShiftCode = r.WorkShift != null ? r.WorkShift.Code : string.Empty,
                WorkShiftName = r.WorkShift != null ? r.WorkShift.Name : string.Empty,
                WorkDate = r.WorkDate,
                CheckInTime = r.CheckInTime,
                CheckOutTime = r.CheckOutTime,
                LateMinutes = r.LateMinutes,
                EarlyMinutes = r.EarlyMinutes,
                WorkHours = r.WorkHours,
                OvertimeHours = r.OvertimeHours,
                Status = r.Status.ToString().ToUpper(),
                ProcessedAt = r.ProcessedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<DailyAttendanceRecordResponseDto>
        {
            Items = items,
            TotalItems = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

    }

    public async Task<DailyAttendanceRecordResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var record = await _context.DailyAttendanceRecords
            .AsNoTracking()
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Include(r => r.WorkShift)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (record == null)
        {
            return null;
        }

        return new DailyAttendanceRecordResponseDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.Employee != null ? record.Employee.EmployeeCode : string.Empty,
            EmployeeFullName = record.Employee != null ? record.Employee.FullName : string.Empty,
            DepartmentName = record.Employee != null && record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
            WorkShiftId = record.WorkShiftId ?? 0,
            WorkShiftCode = record.WorkShift != null ? record.WorkShift.Code : string.Empty,
            WorkShiftName = record.WorkShift != null ? record.WorkShift.Name : string.Empty,
            WorkDate = record.WorkDate,
            CheckInTime = record.CheckInTime,
            CheckOutTime = record.CheckOutTime,
            LateMinutes = record.LateMinutes,
            EarlyMinutes = record.EarlyMinutes,
            WorkHours = record.WorkHours,
            OvertimeHours = record.OvertimeHours,
            Status = record.Status.ToString().ToUpper(),
            ProcessedAt = record.ProcessedAt
        };
    }
}