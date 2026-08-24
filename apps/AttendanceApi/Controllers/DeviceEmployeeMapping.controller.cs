using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.Mappings;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;


namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/device-mappings")]
public class DeviceEmployeeMappingsController : ControllerBase
{
    private readonly IDeviceEmployeeMappingService _mappingService;

    public DeviceEmployeeMappingsController(IDeviceEmployeeMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<DeviceMappingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] DeviceMappingFilterDto filter)
    {
        var result = await _mappingService.GetPagedAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DeviceMappingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var mapping = await _mappingService.GetByIdAsync(id);
        if (mapping == null)
        {
            return NotFound(new { message = $"Không tìm thấy ánh xạ có ID {id}." });
        }
        return Ok(mapping);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DeviceMappingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateDeviceMappingDto dto)
    {
        try
        {
            var created = await _mappingService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        var deleted = await _mappingService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Không tìm thấy ánh xạ có ID {id} để xóa." });
        }
        return NoContent();
    }
}