using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.MonthlyTimesheet;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

public class MonthlyTimesheetService : IMonthlyTimesheetService
{
    private readonly AttendanceDbContext _context;

    public MonthlyTimesheetService(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<MonthlyTimesheetResponseDto>> GetPagedAsync(MonthlyTimesheetFilterDto filter)
    {
        var query = _context.MonthlyTimesheetSummaries
            .Include(m => m.Employee)
                .ThenInclude(e => e!.Department)
            .Include(m => m.Finalizer)
            .AsNoTracking()
            .AsQueryable();

        if (filter.Year.HasValue)
        {
            query = query.Where(m => m.Year == filter.Year.Value);
        }

        if (filter.Month.HasValue)
        {
            query = query.Where(m => m.Month == filter.Month.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(m => m.Employee != null && m.Employee.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(m => m.EmployeeId == filter.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<TimesheetStatus>(filter.Status, true, out var parsedStatus))
        {
            query = query.Where(m => m.Status == parsedStatus);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ThenBy(m => m.EmployeeId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(m => MapToResponseDto(m))
            .ToListAsync();

        return new PagedResultDto<MonthlyTimesheetResponseDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<MonthlyTimesheetResponseDto?> GetByIdAsync(long id)
    {
        var item = await _context.MonthlyTimesheetSummaries
            .Include(m => m.Employee)
                .ThenInclude(e => e!.Department)
            .Include(m => m.Finalizer)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        return item == null ? null : MapToResponseDto(item);
    }

    public async Task<MonthlyTimesheetResponseDto?> GetMyTimesheetAsync(int employeeId, int year, int month)
    {
        var item = await _context.MonthlyTimesheetSummaries
            .Include(m => m.Employee)
                .ThenInclude(e => e!.Department)
            .Include(m => m.Finalizer)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeId && m.Year == year && m.Month == month);

        return item == null ? null : MapToResponseDto(item);
    }

    public async Task<int> LockOrFinalizeTimesheetAsync(LockTimesheetDto dto)
    {
        var action = dto.Action.Trim().ToUpper();
        if (action != "FINALIZE" && action != "LOCK")
        {
            throw new ArgumentException("Hành động không hợp lệ. Chỉ chấp nhận 'FINALIZE' hoặc 'LOCK'.");
        }

        var finalizer = await _context.Employees.FindAsync(dto.FinalizerId);
        if (finalizer == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy người thực hiện (Finalizer) với ID = {dto.FinalizerId}");
        }

        var query = _context.MonthlyTimesheetSummaries
            .Include(m => m.Employee)
            .Where(m => m.Year == dto.Year && m.Month == dto.Month);

        if (dto.DepartmentId.HasValue)
        {
            query = query.Where(m => m.Employee != null && m.Employee.DepartmentId == dto.DepartmentId.Value);
        }

        var summaries = await query.ToListAsync();
        if (summaries.Count == 0)
        {
            throw new KeyNotFoundException($"Không tìm thấy dữ liệu bảng công tháng {dto.Month:D2}/{dto.Year} để thực hiện {action}.");
        }

        var targetStatus = action == "FINALIZE" ? TimesheetStatus.Finalized : TimesheetStatus.Locked;
        var now = DateTime.UtcNow;
        int updatedCount = 0;

        foreach (var summary in summaries)
        {
            if (summary.Status == TimesheetStatus.Locked && targetStatus == TimesheetStatus.Finalized)
            {
                continue; // Không hạ cấp từ LOCKED xuống FINALIZED
            }

            summary.Status = targetStatus;
            summary.FinalizedBy = finalizer.Id;
            summary.FinalizedAt = now;
            summary.UpdatedAt = now;
            updatedCount++;
        }

        await _context.SaveChangesAsync();
        return updatedCount;
    }

    private static MonthlyTimesheetResponseDto MapToResponseDto(MonthlyTimesheetSummary m)
    {
        return new MonthlyTimesheetResponseDto
        {
            Id = m.Id,
            EmployeeId = m.EmployeeId,
            EmployeeCode = m.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = m.Employee?.FullName ?? string.Empty,
            DepartmentName = m.Employee?.Department?.Name,
            Year = m.Year,
            Month = m.Month,
            StandardWorkingDays = m.StandardWorkingDays,
            ActualWorkingDays = m.ActualWorkingDays,
            ActualWorkingHours = m.ActualWorkingHours,
            PaidLeaveDays = m.PaidLeaveDays,
            UnpaidLeaveDays = m.UnpaidLeaveDays,
            AbsentDays = m.AbsentDays,
            LateMinutes = m.LateMinutes,
            EarlyMinutes = m.EarlyMinutes,
            LateOccurrences = m.LateOccurrences,
            EarlyOccurrences = m.EarlyOccurrences,
            OvertimeHours = m.OvertimeHours,
            TotalPayableDays = m.TotalPayableDays,
            Status = m.Status.ToString().ToUpper(),
            FinalizedBy = m.FinalizedBy,
            FinalizerFullName = m.Finalizer?.FullName,
            FinalizedAt = m.FinalizedAt,
            CreatedAt = m.CreatedAt
        };
    }
}