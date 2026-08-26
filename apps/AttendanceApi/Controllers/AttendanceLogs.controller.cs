using System.Text.Json;
using AttendanceApi.DTOs.AttendanceLogs;
using AttendanceApi.DTOs.Common;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/attendance-logs")]
public class AttendanceLogsController : ControllerBase
{
    private readonly IIngestionService _ingestionService;
    private readonly IRawAttendanceLogService _rawAttendanceLogService;

    public AttendanceLogsController(
        IIngestionService ingestionService,
        IRawAttendanceLogService rawAttendanceLogService)
    {
        _ingestionService = ingestionService;
        _rawAttendanceLogService = rawAttendanceLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<RawAttendanceLogResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedLogs([FromQuery] RawAttendanceLogFilterDto filter)
    {
        var result = await _rawAttendanceLogService.GetPagedAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(RawAttendanceLogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _rawAttendanceLogService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy nhật ký chấm công với ID = {id}" });
        }

        return Ok(result);
    }

    [HttpPost("ingest")]
    [ProducesResponseType(typeof(IngestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ingest([FromBody] JsonElement payload)
    {
        try
        {
            var logs = new List<IngestAttendanceLogDto>();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (payload.ValueKind == JsonValueKind.Array)
            {
                var deserializedList = JsonSerializer.Deserialize<List<IngestAttendanceLogDto>>(payload.GetRawText(), jsonOptions);
                if (deserializedList != null)
                {
                    logs.AddRange(deserializedList);
                }
            }
            else if (payload.ValueKind == JsonValueKind.Object)
            {
                var singleLog = JsonSerializer.Deserialize<IngestAttendanceLogDto>(payload.GetRawText(), jsonOptions);
                if (singleLog != null)
                {
                    logs.Add(singleLog);
                }
            }
            else
            {
                return BadRequest(new { message = "Định dạng Payload không hợp lệ. Vui lòng gửi JSON Object hoặc JSON Array." });
            }

            if (logs.Count == 0)
            {
                return BadRequest(new { message = "Danh sách dữ liệu quẹt thẻ rỗng." });
            }

            foreach (var log in logs)
            {
                if (string.IsNullOrWhiteSpace(log.DeviceCode) || string.IsNullOrWhiteSpace(log.DeviceUserId))
                {
                    return BadRequest(new { message = "DeviceCode và DeviceUserId là các trường bắt buộc đối với mỗi bản ghi log." });
                }
            }

            var response = await _ingestionService.IngestLogsAsync(logs);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đã xảy ra lỗi trong quá trình xử lý Ingestion.", detail = ex.Message });
        }
    }
}