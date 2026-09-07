using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.Departments;
using AttendanceApi.DTOs.Devices;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace AttendanceApi.UnitTests.Caching;

public class CachingTests
{
    private static AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AttendanceDbContext(options);
    }

    private static IMemoryCache CreateMemoryCache()
    {
        return new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task DepartmentService_GetAllAsync_CachesResult_And_ReturnsSameData()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var cache = CreateMemoryCache();

        context.Departments.Add(new Department { Code = "IT", Name = "IT Department" });
        await context.SaveChangesAsync();

        var service = new DepartmentService(context, cache);

        // Act - Call 1 (Cache Miss)
        var result1 = await service.GetAllAsync(null);

        // Modify DB directly behind the scene
        context.Departments.Add(new Department { Code = "HR", Name = "HR Department" });
        await context.SaveChangesAsync();

        // Act - Call 2 (Cache Hit - Should return cached 1 department)
        var result2 = await service.GetAllAsync(null);

        // Assert
        Assert.Single(result1);
        Assert.Single(result2);
        Assert.Equal(result1.First().Code, result2.First().Code);
    }

    [Fact]
    public async Task DepartmentService_CreateAsync_InvalidatesCache()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var cache = CreateMemoryCache();

        context.Departments.Add(new Department { Code = "IT", Name = "IT Department" });
        await context.SaveChangesAsync();

        var service = new DepartmentService(context, cache);

        // Act 1: Populate Cache
        var initialList = await service.GetAllAsync(null);
        Assert.Single(initialList);

        // Act 2: Create new Department via service (triggers InvalidateCache)
        await service.CreateAsync(new CreateDepartmentDto { Code = "HR", Name = "HR Department" });

        // Act 3: Call GetAllAsync again (should query DB and return 2 departments)
        var updatedList = await service.GetAllAsync(null);

        // Assert
        Assert.Equal(2, updatedList.Count());
    }

    [Fact]
    public async Task AttendanceDeviceService_GetPagedAsync_CachesResult_And_InvalidatesOnUpdate()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var cache = CreateMemoryCache();

        var device = new AttendanceDevice
        {
            Code = "DEV01",
            Name = "Main Gate",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Status = DeviceStatus.Online
        };
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        var service = new AttendanceDeviceService(context, cache);
        var filter = new DeviceFilterDto { PageNumber = 1, PageSize = 10 };

        // Act 1: Populate cache
        var paged1 = await service.GetPagedAsync(filter);
        Assert.Equal(1, paged1.TotalItems);
        Assert.Equal("Main Gate", paged1.Items.First().Name);

        // Act 2: Update device via service (triggers InvalidateCache)
        await service.UpdateAsync(device.Id, new UpdateDeviceDto
        {
            Name = "Main Gate Updated",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Status = DeviceStatus.Online
        });

        // Act 3: Call GetPagedAsync again
        var paged2 = await service.GetPagedAsync(filter);

        // Assert
        Assert.Equal("Main Gate Updated", paged2.Items.First().Name);
    }
}