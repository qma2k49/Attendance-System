using AttendanceApi.Domain.Entities;
using AttendanceApi.DTOs.Mappings;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class DeviceEmployeeMappingServiceTests
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
    public async Task GetPagedAsync_FilterByDeviceId_ShouldReturnCorrectMappings()
    {
        using var context = CreateInMemoryDbContext();
        var dev1 = new AttendanceDevice { Code = "D01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        var dev2 = new AttendanceDevice { Code = "D02", Name = "Máy 2", IpAddress = "192.168.1.2" };
        var emp = new Employee { EmployeeCode = "E01", FullName = "Nguyễn Văn A", StartDate = new DateOnly(2024, 1, 1) };

        context.AttendanceDevices.AddRange(dev1, dev2);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        context.DeviceEmployeeMappings.AddRange(
            new DeviceEmployeeMapping { DeviceId = dev1.Id, EmployeeId = emp.Id, DeviceUserId = "101" },
            new DeviceEmployeeMapping { DeviceId = dev2.Id, EmployeeId = emp.Id, DeviceUserId = "101" }
        );
        await context.SaveChangesAsync();

        var service = new DeviceEmployeeMappingService(context);
        var filter = new DeviceMappingFilterDto { DeviceId = dev1.Id };

        var result = await service.GetPagedAsync(filter);

        var list = result.Items.ToList();
        Assert.Single(list);
        Assert.Equal("D01", list[0].DeviceCode);
        Assert.Equal("Nguyễn Văn A", list[0].EmployeeFullName);
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_ShouldFilterByDeviceUserIdOrEmployeeName()
    {
        using var context = CreateInMemoryDbContext();
        var dev = new AttendanceDevice { Code = "D01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        var emp1 = new Employee { EmployeeCode = "E01", FullName = "Lê Hoàng Nam", StartDate = new DateOnly(2024, 1, 1) };
        var emp2 = new Employee { EmployeeCode = "E02", FullName = "Trần Thị Mai", StartDate = new DateOnly(2024, 1, 1) };

        context.AttendanceDevices.Add(dev);
        context.Employees.AddRange(emp1, emp2);
        await context.SaveChangesAsync();

        context.DeviceEmployeeMappings.AddRange(
            new DeviceEmployeeMapping { DeviceId = dev.Id, EmployeeId = emp1.Id, DeviceUserId = "1001" },
            new DeviceEmployeeMapping { DeviceId = dev.Id, EmployeeId = emp2.Id, DeviceUserId = "1002" }
        );
        await context.SaveChangesAsync();

        var service = new DeviceEmployeeMappingService(context);
        var filter = new DeviceMappingFilterDto { Keyword = "hoàng nam" };

        var result = await service.GetPagedAsync(filter);

        var list = result.Items.ToList();
        Assert.Single(list);
        Assert.Equal("1001", list[0].DeviceUserId);
    }

    // --- 2. GetByIdAsync ---
    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnMappingWithDetails()
    {
        using var context = CreateInMemoryDbContext();
        var dev = new AttendanceDevice { Code = "D01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        var emp = new Employee { EmployeeCode = "E01", FullName = "Vũ Minh Tuấn", StartDate = new DateOnly(2024, 1, 1) };
        context.AttendanceDevices.Add(dev);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var mapping = new DeviceEmployeeMapping { DeviceId = dev.Id, EmployeeId = emp.Id, DeviceUserId = "999" };
        context.DeviceEmployeeMappings.Add(mapping);
        await context.SaveChangesAsync();

        var service = new DeviceEmployeeMappingService(context);
        var result = await service.GetByIdAsync(mapping.Id);

        Assert.NotNull(result);
        Assert.Equal("999", result.DeviceUserId);
        Assert.Equal("D01", result.DeviceCode);
        Assert.Equal("Vũ Minh Tuấn", result.EmployeeFullName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new DeviceEmployeeMappingService(context);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // --- 3. CreateAsync ---
    [Fact]
    public async Task CreateAsync_ValidData_ShouldCreateSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var dev = new AttendanceDevice { Code = "D01", Name = "Máy Cổng", IpAddress = "192.168.1.1" };
        var emp = new Employee { EmployeeCode = "E01", FullName = "Đặng Thu Hương", StartDate = new DateOnly(2024, 1, 1) };
        context.AttendanceDevices.Add(dev);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var service = new DeviceEmployeeMappingService(context);
        var dto = new CreateDeviceMappingDto
        {
            DeviceId = dev.Id,
            EmployeeId = emp.Id,
            DeviceUserId = " 00123 " // Sẽ được trim
        };

        var result = await service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("00123", result.DeviceUserId);
        Assert.Equal("Máy Cổng", result.DeviceName);
        Assert.Equal("Đặng Thu Hương", result.EmployeeFullName);

        var exists = await context.DeviceEmployeeMappings.AnyAsync(m => m.Id == result.Id);
        Assert.True(exists);
    }

    [Fact]
    public async Task CreateAsync_NonExistingDeviceId_ShouldThrowKeyNotFoundException()
    {
        using var context = CreateInMemoryDbContext();
        var emp = new Employee { EmployeeCode = "E01", FullName = "Test", StartDate = new DateOnly(2024, 1, 1) };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var service = new DeviceEmployeeMappingService(context);
        var dto = new CreateDeviceMappingDto { DeviceId = 999, EmployeeId = emp.Id, DeviceUserId = "101" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_NonExistingEmployeeId_ShouldThrowKeyNotFoundException()
    {
        using var context = CreateInMemoryDbContext();
        var dev = new AttendanceDevice { Code = "D01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        context.AttendanceDevices.Add(dev);
        await context.SaveChangesAsync();

        var service = new DeviceEmployeeMappingService(context);
        var dto = new CreateDeviceMappingDto { DeviceId = dev.Id, EmployeeId = 999, DeviceUserId = "101" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_DuplicateDeviceAndDeviceUserId_ShouldThrowInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var dev = new AttendanceDevice { Code = "D01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        var emp1 = new Employee { EmployeeCode = "E01", FullName = "User 1", StartDate = new DateOnly(2024, 1, 1) };
        var emp2 = new Employee { EmployeeCode = "E02", FullName = "User 2", StartDate = new DateOnly(2024, 1, 1) };

        context.AttendanceDevices.Add(dev);
        context.Employees.AddRange(emp1, emp2);
        await context.SaveChangesAsync();

        context.DeviceEmployeeMappings.Add(new DeviceEmployeeMapping
        {
            DeviceId = dev.Id,
            EmployeeId = emp1.Id,
            DeviceUserId = "1001"
        });
        await context.SaveChangesAsync();

        var service = new DeviceEmployeeMappingService(context);
        var duplicateDto = new CreateDeviceMappingDto
        {
            DeviceId = dev.Id,
            EmployeeId = emp2.Id,
            DeviceUserId = "1001" // Trùng DeviceId + DeviceUserId
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(duplicateDto));
    }

    // --- 4. DeleteAsync ---
    [Fact]
    public async Task DeleteAsync_ExistingId_ShouldRemoveFromDbAndReturnTrue()
    {
        using var context = CreateInMemoryDbContext();
        var dev = new AttendanceDevice { Code = "D01", Name = "Máy 1", IpAddress = "192.168.1.1" };
        var emp = new Employee { EmployeeCode = "E01", FullName = "Test", StartDate = new DateOnly(2024, 1, 1) };
        context.AttendanceDevices.Add(dev);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var mapping = new DeviceEmployeeMapping { DeviceId = dev.Id, EmployeeId = emp.Id, DeviceUserId = "101" };
        context.DeviceEmployeeMappings.Add(mapping);
        await context.SaveChangesAsync();

        var service = new DeviceEmployeeMappingService(context);
        var result = await service.DeleteAsync(mapping.Id);

        Assert.True(result);
        var exists = await context.DeviceEmployeeMappings.AnyAsync(m => m.Id == mapping.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ShouldReturnFalse()
    {
        using var context = CreateInMemoryDbContext();
        var service = new DeviceEmployeeMappingService(context);

        var result = await service.DeleteAsync(999);

        Assert.False(result);
    }
}