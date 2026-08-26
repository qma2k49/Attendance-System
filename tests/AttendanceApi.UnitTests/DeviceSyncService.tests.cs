using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.DeviceSync;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class DeviceSyncServiceTests
{
    private AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task SyncDeviceAsync_ValidDeviceId_ShouldCreateSyncLogAndReturnDto()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var device = new AttendanceDevice { Code = "DEV01", Name = "Máy Cổng", IpAddress = "192.168.1.100" };
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        var ingestionService = new IngestionService(context);
        var logger = NullLogger<DeviceSyncService>.Instance;
        var service = new DeviceSyncService(context, ingestionService, logger);

        // Act
        var result = await service.SyncDeviceAsync(device.Id, "MANUAL_TRIGGER");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(device.Id, result.DeviceId);
        Assert.Equal("SUCCESS", result.Status);

        var logInDb = await context.DeviceSyncLogs.FirstOrDefaultAsync(l => l.DeviceId == device.Id);
        Assert.NotNull(logInDb);
        Assert.Equal(SyncStatus.Success, logInDb.Status);
    }

    [Fact]
    public async Task SyncDeviceAsync_NonExistingDeviceId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var ingestionService = new IngestionService(context);
        var logger = NullLogger<DeviceSyncService>.Instance;
        var service = new DeviceSyncService(context, ingestionService, logger);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SyncDeviceAsync(999));
    }

    [Fact]
    public async Task GetLogsPagedAsync_FilterByDeviceId_ShouldReturnCorrectLogs()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var dev1 = new AttendanceDevice { Code = "DEV01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        var dev2 = new AttendanceDevice { Code = "DEV02", Name = "Máy 2", IpAddress = "192.168.1.2" };
        context.AttendanceDevices.AddRange(dev1, dev2);
        await context.SaveChangesAsync();

        context.DeviceSyncLogs.AddRange(
            new DeviceSyncLog { DeviceId = dev1.Id, Status = SyncStatus.Success },
            new DeviceSyncLog { DeviceId = dev1.Id, Status = SyncStatus.Success },
            new DeviceSyncLog { DeviceId = dev2.Id, Status = SyncStatus.Failed }
        );
        await context.SaveChangesAsync();

        var ingestionService = new IngestionService(context);
        var logger = NullLogger<DeviceSyncService>.Instance;
        var service = new DeviceSyncService(context, ingestionService, logger);

        var filter = new DeviceSyncFilterDto { DeviceId = dev1.Id, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetLogsPagedAsync(filter);

        // Assert
        Assert.Equal(2, result.TotalItems);
        Assert.All(result.Items, item => Assert.Equal("DEV01", item.DeviceCode));
    }
}