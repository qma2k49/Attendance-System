using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.MonthlyTimesheet;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class MonthlyTimesheetServiceTests
{
    private AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task GetPagedAsync_FilterByPeriodAndStatus_ShouldReturnMatchingSummaries()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var emp1 = new Employee { EmployeeCode = "EMP01", FullName = "Nguyen Van A" };
        var emp2 = new Employee { EmployeeCode = "EMP02", FullName = "Tran Van B" };
        context.Employees.AddRange(emp1, emp2);
        await context.SaveChangesAsync();

        context.MonthlyTimesheetSummaries.AddRange(
            new MonthlyTimesheetSummary { EmployeeId = emp1.Id, Year = 2026, Month = 8, Status = TimesheetStatus.Draft },
            new MonthlyTimesheetSummary { EmployeeId = emp2.Id, Year = 2026, Month = 8, Status = TimesheetStatus.Finalized },
            new MonthlyTimesheetSummary { EmployeeId = emp1.Id, Year = 2026, Month = 7, Status = TimesheetStatus.Draft }
        );
        await context.SaveChangesAsync();

        var service = new MonthlyTimesheetService(context);
        var filter = new MonthlyTimesheetFilterDto { Year = 2026, Month = 8, Status = "DRAFT", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetPagedAsync(filter);

        // Assert
        Assert.Equal(1, result.TotalItems);
        Assert.Equal("EMP01", result.Items.First().EmployeeCode);
    }

    [Fact]
    public async Task LockOrFinalizeTimesheetAsync_ValidLockAction_ShouldUpdateStatusAndFinalizer()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var emp = new Employee { EmployeeCode = "EMP01", FullName = "Nguyen Van A" };
        var finalizer = new Employee { EmployeeCode = "HR01", FullName = "Le HR Manager" };
        context.Employees.AddRange(emp, finalizer);
        await context.SaveChangesAsync();

        context.MonthlyTimesheetSummaries.Add(new MonthlyTimesheetSummary
        {
            EmployeeId = emp.Id,
            Year = 2026,
            Month = 8,
            Status = TimesheetStatus.Draft
        });
        await context.SaveChangesAsync();

        var service = new MonthlyTimesheetService(context);
        var lockDto = new LockTimesheetDto
        {
            Year = 2026,
            Month = 8,
            FinalizerId = finalizer.Id,
            Action = "LOCK"
        };

        // Act
        var updatedCount = await service.LockOrFinalizeTimesheetAsync(lockDto);

        // Assert
        Assert.Equal(1, updatedCount);
        var summary = await context.MonthlyTimesheetSummaries.FirstAsync();
        Assert.Equal(TimesheetStatus.Locked, summary.Status);
        Assert.Equal(finalizer.Id, summary.FinalizedBy);
        Assert.NotNull(summary.FinalizedAt);
    }

    [Fact]
    public async Task LockOrFinalizeTimesheetAsync_InvalidAction_ShouldThrowArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new MonthlyTimesheetService(context);
        var lockDto = new LockTimesheetDto { Year = 2026, Month = 8, FinalizerId = 1, Action = "INVALID_ACTION" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.LockOrFinalizeTimesheetAsync(lockDto));
    }
}