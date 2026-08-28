using AttendanceApi.DTOs.MonthlyTimesheet;

namespace AttendanceApi.Services;

public interface ITimesheetAggregationService
{
    Task<AggregateTimesheetResultDto> AggregateMonthlyTimesheetsAsync(
        AggregateTimesheetRequestDto request,
        CancellationToken cancellationToken = default);
}