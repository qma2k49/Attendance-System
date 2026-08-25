using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Devices;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class AttendanceDeviceServiceTests
{
    private AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    // --- 1. GetPagedAsync ---
    [Fact]
    public async Task GetPagedAsync_DefaultPagination_ShouldReturnCorrectPagedResult()
    {
        using var context = CreateInMemoryDbContext();
        context.AttendanceDevices.AddRange(
            new AttendanceDevice { Code = "DEV01", Name = "Máy Cổng Chính", IpAddress = "192.168.1.201" },
            new AttendanceDevice { Code = "DEV02", Name = "Máy Tầng 5", IpAddress = "192.168.1.202" },
            new AttendanceDevice { Code = "DEV03", Name = "Máy Sảnh HCM", IpAddress = "192.168.1.203" }
        );
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context);
        var filter = new DeviceFilterDto { PageNumber = 1, PageSize = 2 };

        var result = await service.GetPagedAsync(filter);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_ShouldFilterByCodeNameOrIp()
    {
        using var context = CreateInMemoryDbContext();
        context.AttendanceDevices.AddRange(
            new AttendanceDevice { Code = "DEV01", Name = "Máy Cổng HN", IpAddress = "192.168.1.201" },
            new AttendanceDevice { Code = "DEV02", Name = "Máy Cổng HCM", IpAddress = "10.0.0.1" }
        );
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context);
        var filter = new DeviceFilterDto { Keyword = "10.0.0" };

        var result = await service.GetPagedAsync(filter);

        var list = result.Items.ToList();
        Assert.Single(list);
        Assert.Equal("DEV02", list[0].Code);
    }

    [Fact]
    public async Task GetPagedAsync_WithStatus_ShouldFilterByStatus()
    {
        using var context = CreateInMemoryDbContext();
        context.AttendanceDevices.AddRange(
            new AttendanceDevice { Code = "DEV01", Name = "Máy 1", IpAddress = "192.168.1.1", Status = DeviceStatus.Online },
            new AttendanceDevice { Code = "DEV02", Name = "Máy 2", IpAddress = "192.168.1.2", Status = DeviceStatus.Offline }
        );
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context);
        var filter = new DeviceFilterDto { Status = DeviceStatus.Offline };

        var result = await service.GetPagedAsync(filter);

        var list = result.Items.ToList();
        Assert.Single(list);
        Assert.Equal("OFFLINE", list[0].Status);
    }

    // --- 2. GetByIdAsync ---
    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnDevice()
    {
        using var context = CreateInMemoryDbContext();
        var device = new AttendanceDevice { Code = "DEV_ZK", Name = "ZKTeco Face", IpAddress = "192.168.1.100" };
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context);
        var result = await service.GetByIdAsync(device.Id);

        Assert.NotNull(result);
        Assert.Equal("DEV_ZK", result.Code);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new AttendanceDeviceService(context);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // --- 3. CreateAsync ---
    [Fact]
    public async Task CreateAsync_ValidData_ShouldCreateSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var service = new AttendanceDeviceService(context);
        var dto = new CreateDeviceDto
        {
            Code = "dev_hn_01",
            Name = "Máy Tầng 1",
            IpAddress = "192.168.1.200",
            Port = 4370,
            SerialNumber = "SN-12345"
        };

        var result = await service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("DEV_HN_01", result.Code);
        Assert.Equal("ONLINE", result.Status);

        var exists = await context.AttendanceDevices.AnyAsync(d => d.Id == result.Id);
        Assert.True(exists);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ShouldThrowInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        context.AttendanceDevices.Add(new AttendanceDevice
        {
            Code = "DEV01",
            Name = "Old Device",
            IpAddress = "192.168.1.1"
        });
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context);
        var dto = new CreateDeviceDto
        {
            Code = "dev01",
            Name = "New Device",
            IpAddress = "192.168.1.2"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_DuplicateSerialNumber_ShouldThrowInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        context.AttendanceDevices.Add(new AttendanceDevice
        {
            Code = "DEV01",
            Name = "Device 1",
            IpAddress = "192.168.1.1",
            SerialNumber = "SN-99999"
        });
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context);
        var dto = new CreateDeviceDto
        {
            Code = "DEV02",
            Name = "Device 2",
            IpAddress = "192.168.1.2",
            SerialNumber = "SN-99999"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
    }

    // --- 4. UpdateAsync ---
    [Fact]
    public async Task UpdateAsync_ValidData_ShouldUpdateSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var device = new AttendanceDevice
        {
            Code = "DEV01",
            Name = "Old Name",
            IpAddress = "192.168.1.1",
            Port = 4370,
            Status = DeviceStatus.Online
        };
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context);
        var dto = new UpdateDeviceDto
        {
            Name = "New Name",
            IpAddress = "192.168.1.50",
            Port = 5005,
            Status = DeviceStatus.Offline
        };

        var result = await service.UpdateAsync(device.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("192.168.1.50", result.IpAddress);
        Assert.Equal(5005, result.Port);
        Assert.Equal("OFFLINE", result.Status);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ShouldReturnNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new AttendanceDeviceService(context);
        var dto = new UpdateDeviceDto { Name = "Test", IpAddress = "192.168.1.1", Status = DeviceStatus.Online };

        var result = await service.UpdateAsync(999, dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateSerialNumberWithOtherDevice_ShouldThrowInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var dev1 = new AttendanceDevice { Code = "DEV01", Name = "Device 1", IpAddress = "192.168.1.1", SerialNumber = "SN-001" };
        var dev2 = new AttendanceDevice { Code = "DEV02", Name = "Device 2", IpAddress = "192.168.1.2", SerialNumber = "SN-002" };
        context.AttendanceDevices.AddRange(dev1, dev2);
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context);
        var dto = new UpdateDeviceDto
        {
            Name = "Device 2 Updated",
            IpAddress = "192.168.1.2",
            SerialNumber = "SN-001", // Trùng SN của dev1
            Status = DeviceStatus.Online
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(dev2.Id, dto));
    }

    // --- 5. DeleteAsync ---
    [Fact]
    public async Task DeleteAsync_ExistingId_ShouldRemoveFromDbAndReturnTrue()
    {
        using var context = CreateInMemoryDbContext();
        var device = new AttendanceDevice { Code = "DEV_DEL", Name = "Device to delete", IpAddress = "192.168.1.99" };
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context);
        var result = await service.DeleteAsync(device.Id);

        Assert.True(result);
        var exists = await context.AttendanceDevices.AnyAsync(d => d.Id == device.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ShouldReturnFalse()
    {
        using var context = CreateInMemoryDbContext();
        var service = new AttendanceDeviceService(context);

        var result = await service.DeleteAsync(999);

        Assert.False(result);
    }
}