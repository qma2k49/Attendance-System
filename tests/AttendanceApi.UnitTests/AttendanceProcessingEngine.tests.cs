using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceProcessing;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AttendanceApi.UnitTests.Services;

public class AttendanceProcessingEngineTests
{
    private static AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    private static ILogger<AttendanceProcessingEngine> CreateMockLogger()
    {
        return NullLogger<AttendanceProcessingEngine>.Instance;
    }

    [Fact]
    public async Task ProcessDailyAttendanceAsync_WhenNoSchedules_ReturnsZeroProcessed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var engine = new AttendanceProcessingEngine(context, CreateMockLogger());
        var request = new ProcessAttendanceRequestDto { WorkDate = new DateOnly(2026, 8, 27) };

        // Act
        var result = await engine.ProcessDailyAttendanceAsync(request);

        // Assert
        Assert.Equal(0, result.TotalEmployeesProcessed);
        Assert.Equal(0, result.SuccessCount);
        Assert.Contains("Không tìm thấy", result.Message);
    }

    [Fact]
    public async Task ProcessDailyAttendanceAsync_StandardShift_CalculatesCheckInAndCheckOut()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var workDate = new DateOnly(2026, 8, 27);

        var employee = new Employee { Id = 1, EmployeeCode = "EMP001", FullName = "Nguyen Hung" };
        var shift = new WorkShift
        {
            Id = 1,
            Code = "HC",
            Name = "Hành chính",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            GracePeriodMinutes = 15,
            BreakStartTime = new TimeOnly(12, 0),
            BreakEndTime = new TimeOnly(13, 0),
            IsOvernight = false
        };
        var schedule = new WorkSchedule { Id = 1, EmployeeId = 1, Employee = employee, WorkShiftId = 1, WorkShift = shift, WorkDate = workDate };
        var mapping = new DeviceEmployeeMapping { Id = 1, EmployeeId = 1, DeviceUserId = "DEV_001" };

        var log1 = new RawAttendanceLog
        {
            Id = 1,
            DeviceUserId = "DEV_001",
            CheckTime = new DateTime(2026, 8, 27, 7, 55, 0, DateTimeKind.Utc),
            ProcessedStatus = ProcessedStatus.Pending
        };
        var log2 = new RawAttendanceLog
        {
            Id = 2,
            DeviceUserId = "DEV_001",
            CheckTime = new DateTime(2026, 8, 27, 17, 5, 0, DateTimeKind.Utc),
            ProcessedStatus = ProcessedStatus.Pending
        };

        context.Employees.Add(employee);
        context.WorkShifts.Add(shift);
        context.WorkSchedules.Add(schedule);
        context.DeviceEmployeeMappings.Add(mapping);
        context.RawAttendanceLogs.AddRange(log1, log2);
        await context.SaveChangesAsync();

        var engine = new AttendanceProcessingEngine(context, CreateMockLogger());

        // Act
        var result = await engine.ProcessDailyAttendanceAsync(new ProcessAttendanceRequestDto { WorkDate = workDate });

        // Assert
        Assert.Equal(1, result.SuccessCount);

        var record = await context.DailyAttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == 1 && r.WorkDate == workDate);
        Assert.NotNull(record);
        Assert.Equal(log1.CheckTime, record.CheckInTime);
        Assert.Equal(log2.CheckTime, record.CheckOutTime);
        Assert.Equal(0, record.LateMinutes);
        Assert.Equal(0, record.EarlyMinutes);
        Assert.Equal(8.17m, record.WorkHours);
        Assert.Equal(DailyAttendanceStatus.Present, record.Status);

        var updatedLogs = await context.RawAttendanceLogs.ToListAsync();
        Assert.All(updatedLogs, l => Assert.Equal(ProcessedStatus.Processed, l.ProcessedStatus));
    }

    [Fact]
    public async Task ProcessDailyAttendanceAsync_LateCheckIn_CalculatesLateMinutes()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var workDate = new DateOnly(2026, 8, 27);

        var employee = new Employee { Id = 2, EmployeeCode = "EMP002", FullName = "Tran An" };
        var shift = new WorkShift
        {
            Id = 2,
            Code = "HC",
            Name = "Hành chính",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            GracePeriodMinutes = 15,
            IsOvernight = false
        };
        var schedule = new WorkSchedule { Id = 2, EmployeeId = 2, Employee = employee, WorkShiftId = 2, WorkShift = shift, WorkDate = workDate };
        var mapping = new DeviceEmployeeMapping { Id = 2, EmployeeId = 2, DeviceUserId = "DEV_002" };

        var log1 = new RawAttendanceLog
        {
            Id = 3,
            DeviceUserId = "DEV_002",
            CheckTime = new DateTime(2026, 8, 27, 8, 30, 0, DateTimeKind.Utc),
            ProcessedStatus = ProcessedStatus.Pending
        };
        var log2 = new RawAttendanceLog
        {
            Id = 4,
            DeviceUserId = "DEV_002",
            CheckTime = new DateTime(2026, 8, 27, 17, 0, 0, DateTimeKind.Utc),
            ProcessedStatus = ProcessedStatus.Pending
        };

        context.Employees.Add(employee);
        context.WorkShifts.Add(shift);
        context.WorkSchedules.Add(schedule);
        context.DeviceEmployeeMappings.Add(mapping);
        context.RawAttendanceLogs.AddRange(log1, log2);
        await context.SaveChangesAsync();

        var engine = new AttendanceProcessingEngine(context, CreateMockLogger());

        // Act
        await engine.ProcessDailyAttendanceAsync(new ProcessAttendanceRequestDto { WorkDate = workDate });

        // Assert
        var record = await context.DailyAttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == 2 && r.WorkDate == workDate);
        Assert.NotNull(record);
        Assert.Equal(30, record.LateMinutes);
        Assert.Equal(0, record.EarlyMinutes);
        Assert.Equal(DailyAttendanceStatus.Late, record.Status);
    }

    [Fact]
    public async Task ProcessDailyAttendanceAsync_OvernightShift_PullsLogsAcrossMidnight()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var workDate = new DateOnly(2026, 8, 27);

        var employee = new Employee { Id = 3, EmployeeCode = "EMP003", FullName = "Le Binh" };
        var shift = new WorkShift
        {
            Id = 3,
            Code = "CA3",
            Name = "Ca Đêm",
            StartTime = new TimeOnly(22, 0),
            EndTime = new TimeOnly(6, 0),
            GracePeriodMinutes = 10,
            IsOvernight = true
        };
        var schedule = new WorkSchedule { Id = 3, EmployeeId = 3, Employee = employee, WorkShiftId = 3, WorkShift = shift, WorkDate = workDate };
        var mapping = new DeviceEmployeeMapping { Id = 3, EmployeeId = 3, DeviceUserId = "DEV_003" };

        var logCheckIn = new RawAttendanceLog
        {
            Id = 5,
            DeviceUserId = "DEV_003",
            CheckTime = new DateTime(2026, 8, 27, 21, 50, 0, DateTimeKind.Utc),
            ProcessedStatus = ProcessedStatus.Pending
        };
        var logCheckOut = new RawAttendanceLog
        {
            Id = 6,
            DeviceUserId = "DEV_003",
            CheckTime = new DateTime(2026, 8, 28, 6, 5, 0, DateTimeKind.Utc),
            ProcessedStatus = ProcessedStatus.Pending
        };

        context.Employees.Add(employee);
        context.WorkShifts.Add(shift);
        context.WorkSchedules.Add(schedule);
        context.DeviceEmployeeMappings.Add(mapping);
        context.RawAttendanceLogs.AddRange(logCheckIn, logCheckOut);
        await context.SaveChangesAsync();

        var engine = new AttendanceProcessingEngine(context, CreateMockLogger());

        // Act
        var result = await engine.ProcessDailyAttendanceAsync(new ProcessAttendanceRequestDto { WorkDate = workDate });

        // Assert
        Assert.Equal(1, result.SuccessCount);
        var record = await context.DailyAttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == 3 && r.WorkDate == workDate);
        Assert.NotNull(record);
        Assert.Equal(logCheckIn.CheckTime, record.CheckInTime);
        Assert.Equal(logCheckOut.CheckTime, record.CheckOutTime);
        Assert.Equal(0, record.LateMinutes);
        Assert.Equal(0, record.EarlyMinutes);
        Assert.Equal(DailyAttendanceStatus.Present, record.Status);
    }

    [Fact]
    public async Task ProcessDailyAttendanceAsync_ConsecutivePunches_DeduplicatesWithinBufferWindow()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var workDate = new DateOnly(2026, 8, 27);

        var employee = new Employee { Id = 4, EmployeeCode = "EMP004", FullName = "Do Cuong" };
        var shift = new WorkShift
        {
            Id = 4,
            Code = "HC",
            Name = "Hành chính",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            GracePeriodMinutes = 0,
            IsOvernight = false
        };
        var schedule = new WorkSchedule { Id = 4, EmployeeId = 4, Employee = employee, WorkShiftId = 4, WorkShift = shift, WorkDate = workDate };
        var mapping = new DeviceEmployeeMapping { Id = 4, EmployeeId = 4, DeviceUserId = "DEV_004" };

        var logs = new List<RawAttendanceLog>
        {
            new() { Id = 10, DeviceUserId = "DEV_004", CheckTime = new DateTime(2026, 8, 27, 7, 58, 0, DateTimeKind.Utc), ProcessedStatus = ProcessedStatus.Pending },
            new() { Id = 11, DeviceUserId = "DEV_004", CheckTime = new DateTime(2026, 8, 27, 7, 58, 30, DateTimeKind.Utc), ProcessedStatus = ProcessedStatus.Pending },
            new() { Id = 12, DeviceUserId = "DEV_004", CheckTime = new DateTime(2026, 8, 27, 7, 59, 0, DateTimeKind.Utc), ProcessedStatus = ProcessedStatus.Pending },
            new() { Id = 13, DeviceUserId = "DEV_004", CheckTime = new DateTime(2026, 8, 27, 17, 1, 0, DateTimeKind.Utc), ProcessedStatus = ProcessedStatus.Pending },
            new() { Id = 14, DeviceUserId = "DEV_004", CheckTime = new DateTime(2026, 8, 27, 17, 1, 40, DateTimeKind.Utc), ProcessedStatus = ProcessedStatus.Pending }
        };

        context.Employees.Add(employee);
        context.WorkShifts.Add(shift);
        context.WorkSchedules.Add(schedule);
        context.DeviceEmployeeMappings.Add(mapping);
        context.RawAttendanceLogs.AddRange(logs);
        await context.SaveChangesAsync();

        var engine = new AttendanceProcessingEngine(context, CreateMockLogger());

        // Act
        await engine.ProcessDailyAttendanceAsync(new ProcessAttendanceRequestDto { WorkDate = workDate });

        // Assert
        var record = await context.DailyAttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == 4 && r.WorkDate == workDate);
        Assert.NotNull(record);
        Assert.Equal(new DateTime(2026, 8, 27, 7, 58, 0, DateTimeKind.Utc), record.CheckInTime);
        Assert.Equal(new DateTime(2026, 8, 27, 17, 1, 0, DateTimeKind.Utc), record.CheckOutTime);
    }

    [Fact]
    public async Task ProcessDailyAttendanceAsync_SinglePunch_IdentifiesCheckInOrCheckOut()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var workDate = new DateOnly(2026, 8, 27);

        var employee = new Employee { Id = 5, EmployeeCode = "EMP005", FullName = "Pham Dung" };
        var shift = new WorkShift
        {
            Id = 5,
            Code = "HC",
            Name = "Hành chính",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            GracePeriodMinutes = 15,
            IsOvernight = false
        };
        var schedule = new WorkSchedule { Id = 5, EmployeeId = 5, Employee = employee, WorkShiftId = 5, WorkShift = shift, WorkDate = workDate };
        var mapping = new DeviceEmployeeMapping { Id = 5, EmployeeId = 5, DeviceUserId = "DEV_005" };

        var singleLog = new RawAttendanceLog
        {
            Id = 20,
            DeviceUserId = "DEV_005",
            CheckTime = new DateTime(2026, 8, 27, 8, 5, 0, DateTimeKind.Utc),
            ProcessedStatus = ProcessedStatus.Pending
        };

        context.Employees.Add(employee);
        context.WorkShifts.Add(shift);
        context.WorkSchedules.Add(schedule);
        context.DeviceEmployeeMappings.Add(mapping);
        context.RawAttendanceLogs.Add(singleLog);
        await context.SaveChangesAsync();

        var engine = new AttendanceProcessingEngine(context, CreateMockLogger());

        // Act
        await engine.ProcessDailyAttendanceAsync(new ProcessAttendanceRequestDto { WorkDate = workDate });

        // Assert
        var record = await context.DailyAttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == 5 && r.WorkDate == workDate);
        Assert.NotNull(record);
        Assert.Equal(singleLog.CheckTime, record.CheckInTime);
        Assert.Null(record.CheckOutTime);
        Assert.Equal(DailyAttendanceStatus.EarlyLeave, record.Status);
    }
}