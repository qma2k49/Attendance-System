using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceAdjustment;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests;

public class AttendanceAdjustmentServiceTests
{
    private static AttendanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ValidAdjustment_ReturnsResponseDto()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var emp = new Employee { Id = 1, EmployeeCode = "EMP001", FullName = "Tran Van B" };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var service = new AttendanceAdjustmentService(context);
        var dto = new CreateAttendanceAdjustmentDto
        {
            EmployeeId = 1,
            WorkDate = new DateOnly(2026, 8, 26),
            AdjustmentType = "FORGOTTEN_CHECKIN",
            AdjustedCheckIn = new DateTime(2026, 8, 26, 8, 0, 0, DateTimeKind.Utc),
            AdjustedCheckOut = new DateTime(2026, 8, 26, 17, 0, 0, DateTimeKind.Utc),
            Reason = "Quên quẹt thẻ lúc vào do máy hỏng"
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.EmployeeId);
        Assert.Equal("FORGOTTEN_CHECKIN", result.AdjustmentType);
        Assert.Equal("PENDING", result.Status);
    }

    [Fact]
    public async Task CancelAsync_NonPendingAdjustment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var adjustment = new AttendanceAdjustment
        {
            Id = 5,
            EmployeeId = 1,
            WorkDate = new DateOnly(2026, 8, 26),
            AdjustmentType = AdjustmentType.ForgottenCheckIn,
            Reason = "Giải trình",
            Status = RequestStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.AttendanceAdjustments.Add(adjustment);
        await context.SaveChangesAsync();

        var service = new AttendanceAdjustmentService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelAsync(5));
    }
}
