using System.Text.Json;
using AttendanceApi.DTOs.AttendanceLogs;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;


namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/attendance-logs")]
public class AttendanceLogsController : ControllerBase
{
    private readonly IIngestionService _ingestionService;

    public AttendanceLogsController(IIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
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

            // Hỗ trợ linh hoạt cả log đơn lẻ { ... } hoặc danh sách [ { ... }, { ... } ]
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

            // Validate sơ bộ các trường bắt buộc
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