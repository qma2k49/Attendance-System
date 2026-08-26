using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceLogs;
using AttendanceApi.Hubs;
using AttendanceApi.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

public class IngestionService : IIngestionService
{
    private readonly AttendanceDbContext _context;
    private readonly IHubContext<AttendanceHub>? _hubContext;

    public IngestionService(AttendanceDbContext context, IHubContext<AttendanceHub>? hubContext = null)
    {
        _context = context;
        _hubContext = hubContext;
    }


    public async Task<IngestResponseDto> IngestLogsAsync(IEnumerable<IngestAttendanceLogDto> logDtos)
    {
        var dtoList = logDtos?.ToList() ?? new List<IngestAttendanceLogDto>();
        if (dtoList.Count == 0)
        {
            return new IngestResponseDto
            {
                TotalReceived = 0,
                TotalInserted = 0,
                TotalSkipped = 0,
                Message = "Không có dữ liệu log nào được gửi lên."
            };
        }

        // 1. Chuẩn hóa mã thiết bị và nạp danh sách thiết bị tương ứng từ DB
        var distinctDeviceCodes = dtoList
            .Select(d => d.DeviceCode.Trim().ToUpper())
            .Distinct()
            .ToList();

        var devices = await _context.AttendanceDevices
            .Where(d => distinctDeviceCodes.Contains(d.Code))
            .ToDictionaryAsync(d => d.Code, d => d);

        // Kiểm tra xem có DeviceCode nào không tồn tại trong DB
        var invalidCodes = distinctDeviceCodes.Where(code => !devices.ContainsKey(code)).ToList();
        if (invalidCodes.Count > 0)
        {
            throw new KeyNotFoundException($"Thiết bị chấm công chưa được đăng ký trong hệ thống: {string.Join(", ", invalidCodes)}");
        }

        int totalInserted = 0;
        int totalSkipped = 0;
        var newLogsToInsert = new List<RawAttendanceLog>();
        var updatedDeviceIds = new HashSet<int>();

        // Set dùng để lọc chống trùng lặp ngay bên trong batch gửi lên (In-memory Deduplication)
        var batchDeduplicationKeys = new HashSet<string>();

        foreach (var dto in dtoList)
        {
            var normalizedCode = dto.DeviceCode.Trim().ToUpper();
            var normalizedUserId = dto.DeviceUserId.Trim();
            var device = devices[normalizedCode];

            // Chuyển CheckTime sang chuẩn UTC để lưu trữ chính xác trên PostgreSQL
            var checkTimeUtc = dto.CheckTime.Kind == DateTimeKind.Utc
                ? dto.CheckTime
                : dto.CheckTime.ToUniversalTime();

            // Khóa định danh 1 lượt quẹt duy nhất: DeviceId_DeviceUserId_CheckTime
            var dedupKey = $"{device.Id}_{normalizedUserId}_{checkTimeUtc.Ticks}";

            // Kiểm tra trùng lặp trong nội bộ batch
            if (!batchDeduplicationKeys.Add(dedupKey))
            {
                totalSkipped++;
                continue;
            }

            // Kiểm tra chống trùng lặp với DB: uq_raw_logs_dedup
            var isExistedInDb = await _context.RawAttendanceLogs.AnyAsync(r =>
                r.DeviceId == device.Id &&
                r.DeviceUserId == normalizedUserId &&
                r.CheckTime == checkTimeUtc);

            if (isExistedInDb)
            {
                totalSkipped++;
                continue;
            }

            var logEntity = new RawAttendanceLog
            {
                DeviceId = device.Id,
                DeviceUserId = normalizedUserId,
                CheckTime = checkTimeUtc,
                VerifyMode = dto.VerifyMode,
                ProcessedStatus = ProcessedStatus.Pending,
                RawPayload = dto.RawPayload,
                CreatedAt = DateTime.UtcNow
            };

            newLogsToInsert.Add(logEntity);
            updatedDeviceIds.Add(device.Id);
            totalInserted++;
        }

        // 2. Lưu các bản ghi mới vào DB
        if (newLogsToInsert.Count > 0)
        {
            await _context.RawAttendanceLogs.AddRangeAsync(newLogsToInsert);
        }

        // 3. Cập nhật LastSyncAt cho các thiết bị vừa đẩy log
        var now = DateTime.UtcNow;
        foreach (var deviceId in updatedDeviceIds)
        {
            var device = devices.Values.First(d => d.Id == deviceId);
            device.LastSyncAt = now;
            device.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        if (_hubContext != null && newLogsToInsert.Count > 0)
        {
            foreach (var log in newLogsToInsert)
            {
                var device = devices.Values.First(d => d.Id == log.DeviceId);
                await _hubContext.Clients.All.SendAsync("ReceiveNewAttendanceLog", new
                {
                    logId = log.Id,
                    deviceId = log.DeviceId,
                    deviceCode = device.Code,
                    deviceName = device.Name,
                    deviceUserId = log.DeviceUserId,
                    checkTime = log.CheckTime,
                    verifyMode = log.VerifyMode.ToString().ToUpper(),
                    createdAt = log.CreatedAt
                });
            }
        }

        return new IngestResponseDto
        {
            TotalReceived = dtoList.Count,
            TotalInserted = totalInserted,
            TotalSkipped = totalSkipped,
            Message = $"Xử lý hoàn tất. Thêm mới: {totalInserted}, Bỏ qua (trùng lặp): {totalSkipped}."
        };
    }
}