using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.MonthlyTimesheet;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/timesheets")]
public class MonthlyTimesheetsController : ControllerBase
{
    private readonly ITimesheetAggregationService _aggregationService;
    private readonly IMonthlyTimesheetService _timesheetService;

    public MonthlyTimesheetsController(
        ITimesheetAggregationService aggregationService,
        IMonthlyTimesheetService timesheetService)
    {
        _aggregationService = aggregationService;
        _timesheetService = timesheetService;
    }

    [HttpPost("aggregate")]
    [ProducesResponseType(typeof(AggregateTimesheetResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Aggregate([FromBody] AggregateTimesheetRequestDto request)
    {
        var result = await _aggregationService.AggregateMonthlyTimesheetsAsync(request);
        return Ok(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(PagedResultDto<MonthlyTimesheetResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] MonthlyTimesheetFilterDto filter)
    {
        var result = await _timesheetService.GetPagedAsync(filter);
        return Ok(result);
    }

    [HttpGet("summary/{id:long}")]
    [ProducesResponseType(typeof(MonthlyTimesheetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _timesheetService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy bảng công tháng với ID = {id}" });
        }
        return Ok(result);
    }

    [HttpGet("summary/me")]
    [ProducesResponseType(typeof(MonthlyTimesheetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyTimesheet(
        [FromQuery] int employeeId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var result = await _timesheetService.GetMyTimesheetAsync(employeeId, year, month);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy dữ liệu bảng công tháng {month:D2}/{year} của nhân viên ID = {employeeId}" });
        }
        return Ok(result);
    }

    [HttpPost("summary/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LockOrFinalize([FromBody] LockTimesheetDto dto)
    {
        try
        {
            var updatedCount = await _timesheetService.LockOrFinalizeTimesheetAsync(dto);
            return Ok(new
            {
                message = $"Đã thực hiện {dto.Action} thành công cho {updatedCount} nhân viên trong kỳ công {dto.Month:D2}/{dto.Year}.",
                totalUpdated = updatedCount
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}