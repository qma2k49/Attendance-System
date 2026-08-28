using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.MonthlyTimesheet;

namespace AttendanceApi.Services;

public interface IMonthlyTimesheetService
{
    Task<PagedResultDto<MonthlyTimesheetResponseDto>> GetPagedAsync(MonthlyTimesheetFilterDto filter);
    Task<MonthlyTimesheetResponseDto?> GetByIdAsync(long id);
    Task<MonthlyTimesheetResponseDto?> GetMyTimesheetAsync(int employeeId, int year, int month);
    Task<int> LockOrFinalizeTimesheetAsync(LockTimesheetDto dto);
}