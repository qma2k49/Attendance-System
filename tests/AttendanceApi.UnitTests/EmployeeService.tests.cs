using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Employees;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class EmployeeServiceTests
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
        context.Employees.AddRange(
            new Employee { EmployeeCode = "EMP001", FullName = "Nguyen Van A", StartDate = new DateOnly(2023, 1, 1) },
            new Employee { EmployeeCode = "EMP002", FullName = "Tran Thi B", StartDate = new DateOnly(2023, 2, 1) },
            new Employee { EmployeeCode = "EMP003", FullName = "Le Van C", StartDate = new DateOnly(2023, 3, 1) }
        );
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var filter = new EmployeeFilterDto { PageNumber = 1, PageSize = 2 };

        var result = await service.GetPagedAsync(filter);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_ShouldFilterByCodeOrName()
    {
        using var context = CreateInMemoryDbContext();
        context.Employees.AddRange(
            new Employee { EmployeeCode = "DEV001", FullName = "Nguyen Van Hung", StartDate = new DateOnly(2023, 1, 1) },
            new Employee { EmployeeCode = "HR002", FullName = "Le Thi Mai", StartDate = new DateOnly(2023, 2, 1) }
        );
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var filter = new EmployeeFilterDto { Keyword = "hung" };

        var result = await service.GetPagedAsync(filter);

        var list = result.Items.ToList();
        Assert.Single(list);
        Assert.Equal("DEV001", list[0].EmployeeCode);
    }

    [Fact]
    public async Task GetPagedAsync_WithDepartmentId_ShouldFilterByDepartment()
    {
        using var context = CreateInMemoryDbContext();
        var dept = new Department { Code = "IT", Name = "IT Department" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        context.Employees.AddRange(
            new Employee { EmployeeCode = "EMP001", FullName = "Nguyen Van A", DepartmentId = dept.Id, StartDate = new DateOnly(2023, 1, 1) },
            new Employee { EmployeeCode = "EMP002", FullName = "Tran Thi B", DepartmentId = null, StartDate = new DateOnly(2023, 2, 1) }
        );
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var filter = new EmployeeFilterDto { DepartmentId = dept.Id };

        var result = await service.GetPagedAsync(filter);

        var list = result.Items.ToList();
        Assert.Single(list);
        Assert.Equal("IT Department", list[0].DepartmentName);
    }

    [Fact]
    public async Task GetPagedAsync_WithStatus_ShouldFilterByStatus()
    {
        using var context = CreateInMemoryDbContext();
        context.Employees.AddRange(
            new Employee { EmployeeCode = "EMP001", FullName = "Nguyen Van A", Status = EmployeeStatus.Active, StartDate = new DateOnly(2023, 1, 1) },
            new Employee { EmployeeCode = "EMP002", FullName = "Tran Thi B", Status = EmployeeStatus.Resigned, StartDate = new DateOnly(2023, 2, 1) }
        );
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var filter = new EmployeeFilterDto { Status = EmployeeStatus.Resigned };

        var result = await service.GetPagedAsync(filter);

        var list = result.Items.ToList();
        Assert.Single(list);
        Assert.Equal("RESIGNED", list[0].Status);
    }

    // --- 2. GetByIdAsync ---
    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnEmployeeWithDepartmentName()
    {
        using var context = CreateInMemoryDbContext();
        var dept = new Department { Code = "HR", Name = "Human Resources" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var emp = new Employee
        {
            EmployeeCode = "EMP100",
            FullName = "Pham Van D",
            DepartmentId = dept.Id,
            StartDate = new DateOnly(2023, 1, 1)
        };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var result = await service.GetByIdAsync(emp.Id);

        Assert.NotNull(result);
        Assert.Equal("EMP100", result.EmployeeCode);
        Assert.Equal("Human Resources", result.DepartmentName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new EmployeeService(context);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // --- 3. CreateAsync ---
    [Fact]
    public async Task CreateAsync_ValidData_ShouldCreateSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var dept = new Department { Code = "IT", Name = "IT Support" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var dto = new CreateEmployeeDto
        {
            EmployeeCode = "emp001",
            FullName = "Le Van Long",
            DepartmentId = dept.Id,
            Position = "Developer",
            StartDate = new DateOnly(2024, 1, 1)
        };

        var result = await service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("EMP001", result.EmployeeCode);
        Assert.Equal("IT Support", result.DepartmentName);

        var inDb = await context.Employees.AnyAsync(e => e.Id == result.Id);
        Assert.True(inDb);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmployeeCode_ShouldThrowInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        context.Employees.Add(new Employee
        {
            EmployeeCode = "EMP001",
            FullName = "Old Employee",
            StartDate = new DateOnly(2023, 1, 1)
        });
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var dto = new CreateEmployeeDto
        {
            EmployeeCode = "emp001",
            FullName = "New Employee",
            StartDate = new DateOnly(2024, 1, 1)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_NonExistingDepartmentId_ShouldThrowKeyNotFoundException()
    {
        using var context = CreateInMemoryDbContext();
        var service = new EmployeeService(context);
        var dto = new CreateEmployeeDto
        {
            EmployeeCode = "EMP002",
            FullName = "Nguyen Van X",
            DepartmentId = 999,
            StartDate = new DateOnly(2024, 1, 1)
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(dto));
    }

    // --- 4. UpdateAsync ---
    [Fact]
    public async Task UpdateAsync_ValidData_ShouldUpdateSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var emp = new Employee
        {
            EmployeeCode = "EMP001",
            FullName = "Old Name",
            StartDate = new DateOnly(2023, 1, 1)
        };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var dto = new UpdateEmployeeDto
        {
            FullName = "New Name",
            Position = "Lead Developer",
            Status = EmployeeStatus.Active,
            EndDate = new DateOnly(2025, 1, 1)
        };

        var result = await service.UpdateAsync(emp.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.FullName);
        Assert.Equal("Lead Developer", result.Position);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ShouldReturnNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new EmployeeService(context);
        var dto = new UpdateEmployeeDto
        {
            FullName = "Test",
            Status = EmployeeStatus.Active
        };

        var result = await service.UpdateAsync(999, dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingDepartmentId_ShouldThrowKeyNotFoundException()
    {
        using var context = CreateInMemoryDbContext();
        var emp = new Employee
        {
            EmployeeCode = "EMP001",
            FullName = "Employee A",
            StartDate = new DateOnly(2023, 1, 1)
        };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var dto = new UpdateEmployeeDto
        {
            FullName = "Employee A",
            DepartmentId = 999,
            Status = EmployeeStatus.Active
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(emp.Id, dto));
    }

    [Fact]
    public async Task UpdateAsync_EndDateBeforeStartDate_ShouldThrowArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        var emp = new Employee
        {
            EmployeeCode = "EMP001",
            FullName = "Employee A",
            StartDate = new DateOnly(2023, 5, 1)
        };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var dto = new UpdateEmployeeDto
        {
            FullName = "Employee A",
            Status = EmployeeStatus.Resigned,
            EndDate = new DateOnly(2023, 1, 1) // Ngày kết thúc trước ngày bắt đầu
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(emp.Id, dto));
    }

    // --- 5. DeleteAsync ---
    [Fact]
    public async Task DeleteAsync_ExistingId_ShouldRemoveFromDbAndReturnTrue()
    {
        using var context = CreateInMemoryDbContext();
        var emp = new Employee
        {
            EmployeeCode = "EMP999",
            FullName = "Delete Me",
            StartDate = new DateOnly(2023, 1, 1)
        };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var service = new EmployeeService(context);
        var result = await service.DeleteAsync(emp.Id);

        Assert.True(result);
        var inDb = await context.Employees.AnyAsync(e => e.Id == emp.Id);
        Assert.False(inDb);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ShouldReturnFalse()
    {
        using var context = CreateInMemoryDbContext();
        var service = new EmployeeService(context);

        var result = await service.DeleteAsync(999);

        Assert.False(result);
    }
}