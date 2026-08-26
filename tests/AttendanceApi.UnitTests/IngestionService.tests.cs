using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceLogs;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class IngestionServiceTests
{
    private AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task IngestLogsAsync_ValidPayload_ShouldInsertLogsAndReturnCorrectCounts()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var device = new AttendanceDevice { Code = "DEV01", Name = "Cổng chính", IpAddress = "192.168.1.201" };
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        var service = new IngestionService(context);
        var logs = new List<IngestAttendanceLogDto>
        {
            new() { DeviceCode = "DEV01", DeviceUserId = "1001", CheckTime = new DateTime(2026, 8, 26, 8, 0, 0, DateTimeKind.Utc) },
            new() { DeviceCode = "DEV01", DeviceUserId = "1002", CheckTime = new DateTime(2026, 8, 26, 8, 5, 0, DateTimeKind.Utc) }
        };

        // Act
        var result = await service.IngestLogsAsync(logs);

        // Assert
        Assert.Equal(2, result.TotalReceived);
        Assert.Equal(2, result.TotalInserted);
        Assert.Equal(0, result.TotalSkipped);

        var countInDb = await context.RawAttendanceLogs.CountAsync();
        Assert.Equal(2, countInDb);

        var updatedDevice = await context.AttendanceDevices.FindAsync(device.Id);
        Assert.NotNull(updatedDevice?.LastSyncAt);
    }

    [Fact]
    public async Task IngestLogsAsync_NonExistingDeviceCode_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new IngestionService(context);
        var logs = new List<IngestAttendanceLogDto>
        {
            new() { DeviceCode = "DEV_UNKNOWN", DeviceUserId = "1001", CheckTime = DateTime.UtcNow }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.IngestLogsAsync(logs));
    }

    [Fact]
    public async Task IngestLogsAsync_DuplicateLog_ShouldSkipAndNotThrowException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var device = new AttendanceDevice { Code = "DEV01", Name = "Cổng chính", IpAddress = "192.168.1.201" };
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        var checkTime = new DateTime(2026, 8, 26, 8, 0, 0, DateTimeKind.Utc);
        context.RawAttendanceLogs.Add(new RawAttendanceLog
        {
            DeviceId = device.Id,
            DeviceUserId = "1001",
            CheckTime = checkTime,
            ProcessedStatus = ProcessedStatus.Pending
        });
        await context.SaveChangesAsync();

        var service = new IngestionService(context);
        var incomingLogs = new List<IngestAttendanceLogDto>
        {
            // Log này đã có trong DB
            new() { DeviceCode = "DEV01", DeviceUserId = "1001", CheckTime = checkTime },
            // Log mới hoàn toàn
            new() { DeviceCode = "DEV01", DeviceUserId = "1002", CheckTime = checkTime }
        };

        // Act
        var result = await service.IngestLogsAsync(incomingLogs);

        // Assert
        Assert.Equal(2, result.TotalReceived);
        Assert.Equal(1, result.TotalInserted);
        Assert.Equal(1, result.TotalSkipped);
        Assert.Equal(2, await context.RawAttendanceLogs.CountAsync());
    }
}