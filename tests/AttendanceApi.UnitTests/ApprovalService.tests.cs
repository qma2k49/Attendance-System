using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Approval;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests;

public class ApprovalServiceTests
{
    private static AttendanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task ApproveOrRejectLeaveRequest_Approve_UpdatesStatusToApproved()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var emp = new Employee { Id = 1, EmployeeCode = "EMP001", FullName = "Nhan Vien 1" };
        var manager = new Employee { Id = 2, EmployeeCode = "MGR001", FullName = "Quan Ly 1" };
        var leaveRequest = new LeaveRequest
        {
            Id = 1,
            EmployeeId = 1,
            LeaveType = LeaveType.Annual,
            FromDate = new DateOnly(2026, 9, 1),
            ToDate = new DateOnly(2026, 9, 2),
            TotalDays = 2,
            Reason = "Xin nghỉ phép",
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Employees.AddRange(emp, manager);
        context.LeaveRequests.Add(leaveRequest);
        await context.SaveChangesAsync();

        var service = new ApprovalService(context, null);
        var dto = new ApprovalActionDto
        {
            ApproverId = 2,
            Action = "APPROVE"
        };

        // Act
        var result = await service.ApproveOrRejectLeaveRequestAsync(1, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("APPROVED", result.Status);
        Assert.Equal(2, result.ApproverId);
        Assert.Equal("Quan Ly 1", result.ApproverFullName);
        Assert.NotNull(result.ApprovedAt);
    }

    [Fact]
    public async Task ApproveOrRejectLeaveRequest_RejectWithoutReason_ThrowsArgumentException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var manager = new Employee { Id = 2, EmployeeCode = "MGR001", FullName = "Quan Ly 1" };
        context.Employees.Add(manager);
        await context.SaveChangesAsync();

        var service = new ApprovalService(context, null);
        var dto = new ApprovalActionDto
        {
            ApproverId = 2,
            Action = "REJECT",
            RejectionReason = "  "
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ApproveOrRejectLeaveRequestAsync(1, dto));
    }
}
