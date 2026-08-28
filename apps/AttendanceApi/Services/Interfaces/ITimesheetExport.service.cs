using AttendanceApi.DTOs.MonthlyTimesheet;

namespace AttendanceApi.Services;

public interface ITimesheetExportService
{
    Task<byte[]> ExportToExcelAsync(TimesheetExportFilterDto filter, CancellationToken cancellationToken = default);
    Task<byte[]> ExportToCsvAsync(TimesheetExportFilterDto filter, CancellationToken cancellationToken = default);
}