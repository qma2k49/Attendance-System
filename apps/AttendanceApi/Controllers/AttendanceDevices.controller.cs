using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.Devices;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;


namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/devices")]
public class AttendanceDevicesController : ControllerBase
{
    private readonly IAttendanceDeviceService _deviceService;

    public AttendanceDevicesController(IAttendanceDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<DeviceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] DeviceFilterDto filter)
    {
        var result = await _deviceService.GetPagedAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DeviceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var device = await _deviceService.GetByIdAsync(id);
        if (device == null)
        {
            return NotFound(new { message = $"Không tìm thấy thiết bị có ID {id}." });
        }
        return Ok(device);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DeviceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateDeviceDto dto)
    {
        try
        {
            var created = await _deviceService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DeviceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceDto dto)
    {
        try
        {
            var updated = await _deviceService.UpdateAsync(id, dto);
            if (updated == null)
            {
                return NotFound(new { message = $"Không tìm thấy thiết bị có ID {id} để cập nhật." });
            }
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _deviceService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Không tìm thấy thiết bị có ID {id} để xóa." });
        }
        return NoContent();
    }
}