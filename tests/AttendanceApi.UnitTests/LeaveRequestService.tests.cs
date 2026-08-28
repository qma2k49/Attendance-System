using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.LeaveRequest;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests;

public class LeaveRequestServiceTests
{
    private static AttendanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsResponseDto()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var emp = new Employee { Id = 1, EmployeeCode = "EMP001", FullName = "Nguyen Van A" };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var service = new LeaveRequestService(context);
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            LeaveType = "ANNUAL",
            FromDate = new DateOnly(2026, 9, 1),
            ToDate = new DateOnly(2026, 9, 3),
            Reason = "Nghỉ phép năm đi du lịch"
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.EmployeeId);
        Assert.Equal(3, result.TotalDays);
        Assert.Equal("PENDING", result.Status);
        Assert.Equal("ANNUAL", result.LeaveType);
    }

    [Fact]
    public async Task CreateAsync_InvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new LeaveRequestService(context);
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            LeaveType = "ANNUAL",
            FromDate = new DateOnly(2026, 9, 5),
            ToDate = new DateOnly(2026, 9, 1),
            Reason = "Ngày kết thúc trước ngày bắt đầu"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CancelAsync_PendingRequest_StatusChangesToCancelled()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var leaveRequest = new LeaveRequest
        {
            Id = 10,
            EmployeeId = 1,
            LeaveType = LeaveType.Annual,
            FromDate = new DateOnly(2026, 9, 1),
            ToDate = new DateOnly(2026, 9, 2),
            TotalDays = 2,
            Reason = "Xin nghỉ",
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.LeaveRequests.Add(leaveRequest);
        await context.SaveChangesAsync();

        var service = new LeaveRequestService(context);

        // Act
        var success = await service.CancelAsync(10);

        // Assert
        Assert.True(success);
        var updated = await context.LeaveRequests.FindAsync(10L);
        Assert.NotNull(updated);
        Assert.Equal(RequestStatus.Cancelled, updated.Status);
    }
}
