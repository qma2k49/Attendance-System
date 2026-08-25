using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.DeviceSync;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Mvc;


namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/v1/device-sync")]
public class DeviceSyncController : ControllerBase
{
    private readonly IDeviceSyncService _syncService;

    public DeviceSyncController(IDeviceSyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost("{deviceId:int}")]
    [ProducesResponseType(typeof(DeviceSyncLogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncSingleDevice(int deviceId)
    {
        try
        {
            var result = await _syncService.SyncDeviceAsync(deviceId, "MANUAL_TRIGGER");
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("sync-all")]
    [ProducesResponseType(typeof(IEnumerable<DeviceSyncLogResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncAllDevices()
    {
        var results = await _syncService.SyncAllActiveDevicesAsync("MANUAL_TRIGGER_ALL");
        return Ok(results);
    }

    [HttpGet("logs")]
    [ProducesResponseType(typeof(PagedResultDto<DeviceSyncLogResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncLogs([FromQuery] DeviceSyncFilterDto filter)
    {
        var result = await _syncService.GetLogsPagedAsync(filter);
        return Ok(result);
    }
}