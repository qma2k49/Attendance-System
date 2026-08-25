using AttendanceApi.Domain.Entities;
using AttendanceApi.DTOs.Departments;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class DepartmentServiceTests
{
    private AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    // --- 1. GetAllAsync ---
    [Fact]
    public async Task GetAllAsync_NoKeyword_ShouldReturnAllDepartments()
    {
        using var context = CreateInMemoryDbContext();
        context.Departments.AddRange(
            new Department { Code = "IT", Name = "Công nghệ thông tin" },
            new Department { Code = "HR", Name = "Hành chính nhân sự" }
        );
        await context.SaveChangesAsync();

        var service = new DepartmentService(context);
        var result = await service.GetAllAsync(null);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithKeyword_ShouldReturnFilteredDepartments()
    {
        using var context = CreateInMemoryDbContext();
        context.Departments.AddRange(
            new Department { Code = "IT", Name = "Phòng Công nghệ" },
            new Department { Code = "HR", Name = "Phòng Nhân sự" }
        );
        await context.SaveChangesAsync();

        var service = new DepartmentService(context);
        var result = await service.GetAllAsync("công nghệ");

        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal("IT", list[0].Code);
    }

    // --- 2. GetByIdAsync ---
    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnDepartment()
    {
        using var context = CreateInMemoryDbContext();
        var dept = new Department { Code = "FIN", Name = "Tài chính" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var service = new DepartmentService(context);
        var result = await service.GetByIdAsync(dept.Id);

        Assert.NotNull(result);
        Assert.Equal("FIN", result.Code);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // --- 3. CreateAsync ---
    [Fact]
    public async Task CreateAsync_ValidData_ShouldCreateSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);
        var dto = new CreateDepartmentDto { Code = "mkt", Name = "Marketing" };

        var result = await service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("MKT", result.Code);
        Assert.Equal("Marketing", result.Name);

        var existsInDb = await context.Departments.AnyAsync(d => d.Id == result.Id);
        Assert.True(existsInDb);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ShouldThrowInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        context.Departments.Add(new Department { Code = "OPS", Name = "Vận hành" });
        await context.SaveChangesAsync();

        var service = new DepartmentService(context);
        var dto = new CreateDepartmentDto { Code = "ops", Name = "Vận hành mới" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
    }

    // --- 4. UpdateAsync ---
    [Fact]
    public async Task UpdateAsync_ExistingId_ShouldUpdateNameAndTimestamp()
    {
        using var context = CreateInMemoryDbContext();
        var dept = new Department { Code = "SALE", Name = "Kinh doanh cũ" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var service = new DepartmentService(context);
        var dto = new UpdateDepartmentDto { Name = "Kinh doanh mới" };

        var result = await service.UpdateAsync(dept.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("Kinh doanh mới", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ShouldReturnNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);
        var dto = new UpdateDepartmentDto { Name = "Tên mới" };

        var result = await service.UpdateAsync(999, dto);

        Assert.Null(result);
    }

    // --- 5. DeleteAsync ---
    [Fact]
    public async Task DeleteAsync_ExistingId_ShouldRemoveFromDbAndReturnTrue()
    {
        using var context = CreateInMemoryDbContext();
        var dept = new Department { Code = "TEMP", Name = "Phòng tạm thời" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var service = new DepartmentService(context);
        var result = await service.DeleteAsync(dept.Id);

        Assert.True(result);
        var exists = await context.Departments.AnyAsync(d => d.Id == dept.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ShouldReturnFalse()
    {
        using var context = CreateInMemoryDbContext();
        var service = new DepartmentService(context);

        var result = await service.DeleteAsync(999);

        Assert.False(result);
    }
}