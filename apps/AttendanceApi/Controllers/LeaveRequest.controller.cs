using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.LeaveRequest;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LeaveRequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
    {
        try
        {
            var result = await _leaveRequestService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<LeaveRequestResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] LeaveRequestFilterDto filter)
    {
        var result = await _leaveRequestService.GetPagedAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(LeaveRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _leaveRequestService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy đơn xin nghỉ phép với ID = {id}" });
        }
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(LeaveRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateLeaveRequestDto dto)
    {
        try
        {
            var result = await _leaveRequestService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(long id)
    {
        try
        {
            await _leaveRequestService.CancelAsync(id);
            return Ok(new { message = $"Đã hủy thành công đơn xin nghỉ phép ID = {id}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}