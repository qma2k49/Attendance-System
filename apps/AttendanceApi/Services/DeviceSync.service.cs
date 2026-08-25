using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceLogs;
using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.DeviceSync;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace AttendanceApi.Services;

public class DeviceSyncService : IDeviceSyncService
{
    private readonly AttendanceDbContext _context;
    private readonly IIngestionService _ingestionService;
    private readonly ILogger<DeviceSyncService> _logger;

    public DeviceSyncService(
        AttendanceDbContext context,
        IIngestionService ingestionService,
        ILogger<DeviceSyncService> logger)
    {
        _context = context;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    public async Task<DeviceSyncLogResponseDto> SyncDeviceAsync(int deviceId, string syncType = "MANUAL_TRIGGER")
    {
        var device = await _context.AttendanceDevices.FindAsync(deviceId);
        if (device == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy thiết bị có ID = {deviceId}");
        }

        var syncLog = new DeviceSyncLog
        {
            DeviceId = device.Id,
            SyncType = syncType,
            SyncedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Bắt đầu đồng bộ thiết bị {DeviceCode} ({Ip}:{Port})", device.Code, device.IpAddress, device.Port);

            // Giả lập kéo log từ SDK thiết bị (hoặc tích hợp SDK ZKTeco/Hikvision thật)
            var pulledLogs = await MockPullLogsFromDeviceAsync(device);

            syncLog.RecordsPulled = pulledLogs.Count;

            if (pulledLogs.Count > 0)
            {
                var ingestResult = await _ingestionService.IngestLogsAsync(pulledLogs);
                syncLog.RecordsInserted = ingestResult.TotalInserted;
            }
            else
            {
                syncLog.RecordsInserted = 0;
            }

            syncLog.Status = SyncStatus.Success;
            device.Status = DeviceStatus.Online;
            device.LastSyncAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đồng bộ thất bại cho thiết bị {DeviceCode}: {Message}", device.Code, ex.Message);
            syncLog.Status = SyncStatus.Failed;
            syncLog.ErrorMessage = ex.Message;
            device.Status = DeviceStatus.Error;
            device.UpdatedAt = DateTime.UtcNow;
        }

        _context.DeviceSyncLogs.Add(syncLog);
        await _context.SaveChangesAsync();

        return new DeviceSyncLogResponseDto
        {
            Id = syncLog.Id,
            DeviceId = device.Id,
            DeviceCode = device.Code,
            DeviceName = device.Name,
            SyncType = syncLog.SyncType,
            RecordsPulled = syncLog.RecordsPulled,
            RecordsInserted = syncLog.RecordsInserted,
            Status = syncLog.Status == SyncStatus.PartialSuccess ? "PARTIAL_SUCCESS" : syncLog.Status.ToString().ToUpper(),
            ErrorMessage = syncLog.ErrorMessage,
            SyncedAt = syncLog.SyncedAt
        };
    }

    public async Task<IEnumerable<DeviceSyncLogResponseDto>> SyncAllActiveDevicesAsync(string syncType = "AUTO_SCHEDULED")
    {
        var activeDevices = await _context.AttendanceDevices
            .Where(d => d.Status != DeviceStatus.Offline)
            .ToListAsync();

        var results = new List<DeviceSyncLogResponseDto>();

        foreach (var device in activeDevices)
        {
            var result = await SyncDeviceAsync(device.Id, syncType);
            results.Add(result);
        }

        return results;
    }

    public async Task<PagedResultDto<DeviceSyncLogResponseDto>> GetLogsPagedAsync(DeviceSyncFilterDto filter)
    {
        var query = _context.DeviceSyncLogs
            .Include(l => l.AttendanceDevice)
            .AsNoTracking()
            .AsQueryable();

        if (filter.DeviceId.HasValue)
        {
            query = query.Where(l => l.DeviceId == filter.DeviceId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(l => l.Status == filter.Status.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.SyncedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new DeviceSyncLogResponseDto
            {
                Id = l.Id,
                DeviceId = l.DeviceId,
                DeviceCode = l.AttendanceDevice != null ? l.AttendanceDevice.Code : string.Empty,
                DeviceName = l.AttendanceDevice != null ? l.AttendanceDevice.Name : string.Empty,
                SyncType = l.SyncType,
                RecordsPulled = l.RecordsPulled,
                RecordsInserted = l.RecordsInserted,
                Status = l.Status == SyncStatus.PartialSuccess ? "PARTIAL_SUCCESS" : l.Status.ToString().ToUpper(),
                ErrorMessage = l.ErrorMessage,
                SyncedAt = l.SyncedAt
            })
            .ToListAsync();

        return new PagedResultDto<DeviceSyncLogResponseDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

    }

    private Task<List<IngestAttendanceLogDto>> MockPullLogsFromDeviceAsync(AttendanceDevice device)
    {
        // Khi tích hợp SDK thật, hàm này sẽ mở TCP Socket đến IP:Port của máy và lấy attendance logs
        return Task.FromResult(new List<IngestAttendanceLogDto>());
    }
}