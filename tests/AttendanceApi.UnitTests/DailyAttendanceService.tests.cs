using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.DailyAttendance;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class DailyAttendanceServiceTests
{
    private static AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task GetPagedAsync_WithStatusFilter_ReturnsFilteredRecords()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var department = new Department { Id = 1, Name = "Phòng IT" };
        var employee = new Employee { Id = 1, EmployeeCode = "EMP101", FullName = "Vu Hung", DepartmentId = 1, Department = department };
        var shift = new WorkShift { Id = 1, Code = "HC", Name = "Hành chính" };

        var records = new List<DailyAttendanceRecord>
        {
            new() { Id = 1, EmployeeId = 1, Employee = employee, WorkShiftId = 1, WorkShift = shift, WorkDate = new DateOnly(2026, 8, 25), Status = DailyAttendanceStatus.Present },
            new() { Id = 2, EmployeeId = 1, Employee = employee, WorkShiftId = 1, WorkShift = shift, WorkDate = new DateOnly(2026, 8, 26), Status = DailyAttendanceStatus.Late },
            new() { Id = 3, EmployeeId = 1, Employee = employee, WorkShiftId = 1, WorkShift = shift, WorkDate = new DateOnly(2026, 8, 27), Status = DailyAttendanceStatus.Late }
        };

        context.Departments.Add(department);
        context.Employees.Add(employee);
        context.WorkShifts.Add(shift);
        context.DailyAttendanceRecords.AddRange(records);
        await context.SaveChangesAsync();

        var service = new DailyAttendanceService(context);
        var filter = new DailyAttendanceFilterDto
        {
            Status = "LATE",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var pagedResult = await service.GetPagedAsync(filter);

        // Assert
        Assert.NotNull(pagedResult);
        Assert.Equal(2, pagedResult.TotalItems);
        Assert.Equal(2, pagedResult.Items.Count());
        Assert.All(pagedResult.Items, item => Assert.Equal("LATE", item.Status));
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecordExists_ReturnsResponseDtoWithEmployeeAndShiftDetails()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var department = new Department { Id = 2, Name = "Phòng Kế Toán" };
        var employee = new Employee { Id = 2, EmployeeCode = "EMP202", FullName = "Nguyen Mai", DepartmentId = 2, Department = department };
        var shift = new WorkShift { Id = 2, Code = "CA1", Name = "Ca Sáng" };

        var record = new DailyAttendanceRecord
        {
            Id = 10,
            EmployeeId = 2,
            Employee = employee,
            WorkShiftId = 2,
            WorkShift = shift,
            WorkDate = new DateOnly(2026, 8, 27),
            CheckInTime = new DateTime(2026, 8, 27, 8, 0, 0, DateTimeKind.Utc),
            CheckOutTime = new DateTime(2026, 8, 27, 17, 0, 0, DateTimeKind.Utc),
            WorkHours = 8.0m,
            Status = DailyAttendanceStatus.Present
        };

        context.Departments.Add(department);
        context.Employees.Add(employee);
        context.WorkShifts.Add(shift);
        context.DailyAttendanceRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new DailyAttendanceService(context);

        // Act
        var result = await service.GetByIdAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("EMP202", result.EmployeeCode);
        Assert.Equal("Nguyen Mai", result.EmployeeFullName);
        Assert.Equal("Phòng Kế Toán", result.DepartmentName);
        Assert.Equal("CA1", result.WorkShiftCode);
        Assert.Equal("Ca Sáng", result.WorkShiftName);
    }
}