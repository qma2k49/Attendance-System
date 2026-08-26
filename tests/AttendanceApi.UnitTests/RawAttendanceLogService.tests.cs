using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceLogs;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class RawAttendanceLogServiceTests
{
    private AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task GetPagedAsync_FilterByDateRange_ShouldReturnLogsInRange()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var device = new AttendanceDevice { Code = "DEV01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        context.RawAttendanceLogs.AddRange(
            new RawAttendanceLog { DeviceId = device.Id, DeviceUserId = "101", CheckTime = new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc) },
            new RawAttendanceLog { DeviceId = device.Id, DeviceUserId = "102", CheckTime = new DateTime(2026, 8, 25, 8, 0, 0, DateTimeKind.Utc) },
            new RawAttendanceLog { DeviceId = device.Id, DeviceUserId = "103", CheckTime = new DateTime(2026, 8, 30, 8, 0, 0, DateTimeKind.Utc) }
        );
        await context.SaveChangesAsync();

        var service = new RawAttendanceLogService(context);
        var filter = new RawAttendanceLogFilterDto
        {
            FromDate = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2026, 8, 26, 23, 59, 59, DateTimeKind.Utc)
        };

        // Act
        var result = await service.GetPagedAsync(filter);

        // Assert
        Assert.Equal(1, result.TotalItems);
        var item = result.Items.First();
        Assert.Equal("102", item.DeviceUserId);
    }

    [Fact]
    public async Task GetPagedAsync_WithMapping_ShouldAttachEmployeeAndDepartmentInfo()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var dept = new Department { Code = "IT", Name = "Phòng IT" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "EMP001", FullName = "Nguyen Van A", DepartmentId = dept.Id, StartDate = new DateOnly(2025, 1, 1) };
        var device = new AttendanceDevice { Code = "DEV01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        context.Employees.Add(emp);
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        context.DeviceEmployeeMappings.Add(new DeviceEmployeeMapping
        {
            DeviceId = device.Id,
            EmployeeId = emp.Id,
            DeviceUserId = "1001"
        });

        context.RawAttendanceLogs.Add(new RawAttendanceLog
        {
            DeviceId = device.Id,
            DeviceUserId = "1001",
            CheckTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new RawAttendanceLogService(context);
        var filter = new RawAttendanceLogFilterDto();

        // Act
        var result = await service.GetPagedAsync(filter);

        // Assert
        var item = result.Items.First();
        Assert.Equal("EMP001", item.EmployeeCode);
        Assert.Equal("Nguyen Van A", item.EmployeeFullName);
        Assert.Equal("Phòng IT", item.DepartmentName);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnDetailDto()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var device = new AttendanceDevice { Code = "DEV01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        context.AttendanceDevices.Add(device);
        await context.SaveChangesAsync();

        var log = new RawAttendanceLog
        {
            DeviceId = device.Id,
            DeviceUserId = "1001",
            CheckTime = DateTime.UtcNow,
            VerifyMode = VerifyModeEnum.Fingerprint,
            ProcessedStatus = ProcessedStatus.Pending
        };
        context.RawAttendanceLogs.Add(log);
        await context.SaveChangesAsync();

        var service = new RawAttendanceLogService(context);

        // Act
        var result = await service.GetByIdAsync(log.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(log.Id, result.Id);
        Assert.Equal("DEV01", result.DeviceCode);
        Assert.Equal("PENDING", result.ProcessedStatus);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new RawAttendanceLogService(context);

        // Act
        var result = await service.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }
}