using AttendanceApi.DTOs.MonthlyTimesheet;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/timesheets")]
public class MonthlyTimesheetsController : ControllerBase
{
    private readonly ITimesheetAggregationService _aggregationService;

    public MonthlyTimesheetsController(ITimesheetAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    [HttpPost("aggregate")]
    [ProducesResponseType(typeof(AggregateTimesheetResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AggregateMonthlyTimesheets(
        [FromBody] AggregateTimesheetRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _aggregationService.AggregateMonthlyTimesheetsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
