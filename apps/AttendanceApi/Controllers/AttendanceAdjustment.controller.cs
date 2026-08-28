using AttendanceApi.DTOs.Approval;
using AttendanceApi.DTOs.AttendanceAdjustment;
using AttendanceApi.DTOs.Common;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/attendance-adjustments")]
public class AttendanceAdjustmentsController : ControllerBase
{
    private readonly IAttendanceAdjustmentService _adjustmentService;
    private readonly IApprovalService _approvalService;

    public AttendanceAdjustmentsController(
        IAttendanceAdjustmentService adjustmentService,
        IApprovalService approvalService)
    {
        _adjustmentService = adjustmentService;
        _approvalService = approvalService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AttendanceAdjustmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceAdjustmentDto dto)
    {
        try
        {
            var result = await _adjustmentService.CreateAsync(dto);
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
    [ProducesResponseType(typeof(PagedResultDto<AttendanceAdjustmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] AttendanceAdjustmentFilterDto filter)
    {
        var result = await _adjustmentService.GetPagedAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AttendanceAdjustmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _adjustmentService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound(new { message = $"Không tìm thấy đơn giải trình với ID = {id}" });
        }
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(AttendanceAdjustmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateAttendanceAdjustmentDto dto)
    {
        try
        {
            var result = await _adjustmentService.UpdateAsync(id, dto);
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
            await _adjustmentService.CancelAsync(id);
            return Ok(new { message = $"Đã hủy thành công đơn giải trình ID = {id}" });
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

    [HttpPost("{id:long}/approve")]
    [ProducesResponseType(typeof(AttendanceAdjustmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveOrReject(long id, [FromBody] ApprovalActionDto dto)
    {
        try
        {
            var result = await _approvalService.ApproveOrRejectAdjustmentAsync(id, dto);
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
}