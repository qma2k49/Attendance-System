using AttendanceApi.DTOs.AttendanceProcessing;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/daily-attendance")]
public class AttendanceProcessingController : ControllerBase
{
    private readonly IAttendanceProcessingEngine _processingEngine;

    public AttendanceProcessingController(IAttendanceProcessingEngine processingEngine)
    {
        _processingEngine = processingEngine;
    }

    [HttpPost("process")]
    [ProducesResponseType(typeof(ProcessAttendanceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessDailyAttendance(
        [FromBody] ProcessAttendanceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.WorkDate == default)
        {
            return BadRequest(new { message = "WorkDate là trường bắt buộc." });
        }

        var result = await _processingEngine.ProcessDailyAttendanceAsync(request, cancellationToken);
        return Ok(result);
    }
}
