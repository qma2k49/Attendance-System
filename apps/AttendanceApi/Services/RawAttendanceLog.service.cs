using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceLogs;
using AttendanceApi.DTOs.Common;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace AttendanceApi.Services;

public class RawAttendanceLogService : IRawAttendanceLogService
{
    private readonly AttendanceDbContext _context;

    public RawAttendanceLogService(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<RawAttendanceLogResponseDto>> GetPagedAsync(RawAttendanceLogFilterDto filter)
    {
        var query = from log in _context.RawAttendanceLogs.AsNoTracking()
                    join device in _context.AttendanceDevices.AsNoTracking() on log.DeviceId equals device.Id
                    join mapping in _context.DeviceEmployeeMappings.AsNoTracking()
                        on new { log.DeviceId, log.DeviceUserId } equals new { mapping.DeviceId, mapping.DeviceUserId } into mappingGroup
                    from mapping in mappingGroup.DefaultIfEmpty()
                    join employee in _context.Employees.AsNoTracking() on mapping.EmployeeId equals employee.Id into empGroup
                    from employee in empGroup.DefaultIfEmpty()
                    join department in _context.Departments.AsNoTracking() on employee.DepartmentId equals department.Id into deptGroup
                    from department in deptGroup.DefaultIfEmpty()
                    select new
                    {
                        Log = log,
                        DeviceCode = device.Code,
                        DeviceName = device.Name,
                        EmployeeId = employee != null ? (int?)employee.Id : null,
                        EmployeeCode = employee != null ? employee.EmployeeCode : null,
                        EmployeeFullName = employee != null ? employee.FullName : null,
                        DepartmentName = department != null ? department.Name : null
                    };

        // 1. Lọc theo DeviceId
        if (filter.DeviceId.HasValue)
        {
            query = query.Where(x => x.Log.DeviceId == filter.DeviceId.Value);
        }

        // 2. Lọc theo DeviceUserId
        if (!string.IsNullOrWhiteSpace(filter.DeviceUserId))
        {
            var searchUserId = filter.DeviceUserId.Trim();
            query = query.Where(x => x.Log.DeviceUserId == searchUserId);
        }

        // 3. Lọc theo EmployeeId
        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == filter.EmployeeId.Value);
        }

        // 4. Lọc theo khoảng thời gian CheckTime
        if (filter.FromDate.HasValue)
        {
            var fromUtc = filter.FromDate.Value.Kind == DateTimeKind.Utc 
                ? filter.FromDate.Value 
                : filter.FromDate.Value.ToUniversalTime();
            query = query.Where(x => x.Log.CheckTime >= fromUtc);
        }

        if (filter.ToDate.HasValue)
        {
            var toUtc = filter.ToDate.Value.Kind == DateTimeKind.Utc 
                ? filter.ToDate.Value 
                : filter.ToDate.Value.ToUniversalTime();
            query = query.Where(x => x.Log.CheckTime <= toUtc);
        }

        // 5. Lọc theo ProcessedStatus
        if (filter.ProcessedStatus.HasValue)
        {
            query = query.Where(x => x.Log.ProcessedStatus == filter.ProcessedStatus.Value);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        var items = await query
            .OrderByDescending(x => x.Log.CheckTime)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new RawAttendanceLogResponseDto
            {
                Id = x.Log.Id,
                DeviceId = x.Log.DeviceId,
                DeviceCode = x.DeviceCode,
                DeviceName = x.DeviceName,
                DeviceUserId = x.Log.DeviceUserId,
                EmployeeId = x.EmployeeId,
                EmployeeCode = x.EmployeeCode,
                EmployeeFullName = x.EmployeeFullName,
                DepartmentName = x.DepartmentName,
                CheckTime = x.Log.CheckTime,
                VerifyMode = x.Log.VerifyMode.ToString().ToUpper(),
                ProcessedStatus = x.Log.ProcessedStatus.ToString().ToUpper(),
                RawPayload = x.Log.RawPayload,
                CreatedAt = x.Log.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<RawAttendanceLogResponseDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }


    public async Task<RawAttendanceLogResponseDto?> GetByIdAsync(long id)
    {
        var result = await (from log in _context.RawAttendanceLogs.AsNoTracking()
                            where log.Id == id
                            join device in _context.AttendanceDevices.AsNoTracking() on log.DeviceId equals device.Id
                            join mapping in _context.DeviceEmployeeMappings.AsNoTracking()
                                on new { log.DeviceId, log.DeviceUserId } equals new { mapping.DeviceId, mapping.DeviceUserId } into mappingGroup
                            from mapping in mappingGroup.DefaultIfEmpty()
                            join employee in _context.Employees.AsNoTracking() on mapping.EmployeeId equals employee.Id into empGroup
                            from employee in empGroup.DefaultIfEmpty()
                            join department in _context.Departments.AsNoTracking() on employee.DepartmentId equals department.Id into deptGroup
                            from department in deptGroup.DefaultIfEmpty()
                            select new RawAttendanceLogResponseDto
                            {
                                Id = log.Id,
                                DeviceId = log.DeviceId,
                                DeviceCode = device.Code,
                                DeviceName = device.Name,
                                DeviceUserId = log.DeviceUserId,
                                EmployeeId = employee != null ? (int?)employee.Id : null,
                                EmployeeCode = employee != null ? employee.EmployeeCode : null,
                                EmployeeFullName = employee != null ? employee.FullName : null,
                                DepartmentName = department != null ? department.Name : null,
                                CheckTime = log.CheckTime,
                                VerifyMode = log.VerifyMode.ToString().ToUpper(),
                                ProcessedStatus = log.ProcessedStatus.ToString().ToUpper(),
                                RawPayload = log.RawPayload,
                                CreatedAt = log.CreatedAt
                            })
                            .FirstOrDefaultAsync();

        return result;
    }
}