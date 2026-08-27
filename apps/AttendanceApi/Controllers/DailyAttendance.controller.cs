using AttendanceApi.DTOs.AttendanceProcessing;
using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.DailyAttendance;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/daily-attendance")]
public class DailyAttendanceController : ControllerBase
{
    private readonly IDailyAttendanceService _dailyAttendanceService;
    private readonly IAttendanceProcessingEngine _processingEngine;

    public DailyAttendanceController(
        IDailyAttendanceService dailyAttendanceService,
        IAttendanceProcessingEngine processingEngine)
    {
        _dailyAttendanceService = dailyAttendanceService;
        _processingEngine = processingEngine;
    }

    /// <summary>
    /// Tra cứu danh sách bảng công ngày có phân trang và bộ lọc.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<DailyAttendanceRecordResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] DailyAttendanceFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _dailyAttendanceService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một bản ghi công ngày theo ID.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(DailyAttendanceRecordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken cancellationToken)
    {
        var record = await _dailyAttendanceService.GetByIdAsync(id, cancellationToken);
        if (record == null)
        {
            return NotFound(new { message = $"Không tìm thấy bản ghi công ngày với ID {id}." });
        }

        return Ok(record);
    }

    /// <summary>
    /// Kích hoạt tính toán công ngày thủ công.
    /// </summary>
    [HttpPost("process")]
    [HttpPost("reprocess")]
    [ProducesResponseType(typeof(ProcessAttendanceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessDailyAttendance([FromBody] ProcessAttendanceRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || request.WorkDate == default)
        {
            return BadRequest(new { message = "WorkDate là trường bắt buộc." });
        }

        var result = await _processingEngine.ProcessDailyAttendanceAsync(request, cancellationToken);
        return Ok(result);
    }
}