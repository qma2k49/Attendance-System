using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.MonthlyTimesheet;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class TimesheetAggregationServiceTests
{
    private AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task AggregateMonthlyTimesheets_ValidData_ShouldCalculateAccurateMetrics()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var emp = new Employee { EmployeeCode = "EMP01", FullName = "Nguyen Van A" };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var year = 2026;
        var month = 8;

        // Ngày 1: Present (8h)
        context.DailyAttendanceRecords.Add(new DailyAttendanceRecord
        {
            EmployeeId = emp.Id,
            WorkDate = new DateOnly(2026, 8, 1),
            Status = DailyAttendanceStatus.Present,
            WorkHours = 8.00m,
            LateMinutes = 0,
            EarlyMinutes = 0
        });

        // Ngày 2: Late (7.5h, Late 15m)
        context.DailyAttendanceRecords.Add(new DailyAttendanceRecord
        {
            EmployeeId = emp.Id,
            WorkDate = new DateOnly(2026, 8, 2),
            Status = DailyAttendanceStatus.Late,
            WorkHours = 7.50m,
            LateMinutes = 15,
            EarlyMinutes = 0
        });

        // Ngày 3: Absent
        context.DailyAttendanceRecords.Add(new DailyAttendanceRecord
        {
            EmployeeId = emp.Id,
            WorkDate = new DateOnly(2026, 8, 3),
            Status = DailyAttendanceStatus.Absent,
            WorkHours = 0.00m
        });

        // Ngày 4: OnLeave (Phép năm có lương)
        context.DailyAttendanceRecords.Add(new DailyAttendanceRecord
        {
            EmployeeId = emp.Id,
            WorkDate = new DateOnly(2026, 8, 4),
            Status = DailyAttendanceStatus.OnLeave,
            WorkHours = 0.00m
        });

        context.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = emp.Id,
            LeaveType = LeaveType.Annual,
            FromDate = new DateOnly(2026, 8, 4),
            ToDate = new DateOnly(2026, 8, 4),
            Status = RequestStatus.Approved,
            Reason = "Nghỉ phép năm"
        });

        await context.SaveChangesAsync();

        var logger = NullLogger<TimesheetAggregationService>.Instance;
        var service = new TimesheetAggregationService(context, logger);
        var request = new AggregateTimesheetRequestDto
        {
            Year = year,
            Month = month,
            EmployeeId = emp.Id,
            StandardWorkingDays = 22.0m
        };

        // Act
        var result = await service.AggregateMonthlyTimesheetsAsync(request);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);

        var summary = await context.MonthlyTimesheetSummaries.FirstOrDefaultAsync(m => m.EmployeeId == emp.Id && m.Year == year && m.Month == month);
        Assert.NotNull(summary);
        Assert.Equal(2.0m, summary.ActualWorkingDays);       // Ngày 1 + Ngày 2
        Assert.Equal(15.50m, summary.ActualWorkingHours);    // 8.0 + 7.5
        Assert.Equal(1.0m, summary.PaidLeaveDays);          // Ngày 4
        Assert.Equal(1.0m, summary.AbsentDays);             // Ngày 3
        Assert.Equal(15, summary.LateMinutes);              // 15m
        Assert.Equal(1, summary.LateOccurrences);           // 1 lần muộn
        Assert.Equal(3.0m, summary.TotalPayableDays);       // 2.0 (Actual) + 1.0 (PaidLeave)
    }

    [Fact]
    public async Task AggregateMonthlyTimesheets_LockedSummary_ShouldSkipAndNotOverwrite()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var emp = new Employee { EmployeeCode = "EMP01", FullName = "Nguyen Van A" };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var year = 2026;
        var month = 8;

        context.MonthlyTimesheetSummaries.Add(new MonthlyTimesheetSummary
        {
            EmployeeId = emp.Id,
            Year = year,
            Month = month,
            ActualWorkingDays = 20.0m,
            Status = TimesheetStatus.Locked
        });
        await context.SaveChangesAsync();

        var logger = NullLogger<TimesheetAggregationService>.Instance;
        var service = new TimesheetAggregationService(context, logger);
        var request = new AggregateTimesheetRequestDto { Year = year, Month = month, EmployeeId = emp.Id };

        // Act
        var result = await service.AggregateMonthlyTimesheetsAsync(request);

        // Assert
        Assert.Equal(1, result.SkippedLockedCount);
        var summary = await context.MonthlyTimesheetSummaries.FirstOrDefaultAsync(m => m.EmployeeId == emp.Id && m.Year == year && m.Month == month);
        Assert.NotNull(summary);
        Assert.Equal(20.0m, summary.ActualWorkingDays); // Giữ nguyên không bị ghi đè
    }
}