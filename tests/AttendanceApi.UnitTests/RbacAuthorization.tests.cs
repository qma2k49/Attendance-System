using AttendanceApi.Common.Constants;
using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Approval;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AttendanceApi.UnitTests.Security;

public class RbacAuthorizationTests
{
    private AttendanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AttendanceDbContext(options);
    }

    [Fact]
    public async Task Manager_ApproveRequest_DifferentDepartment_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var itDept = new Department { Code = "IT", Name = "IT Dept" };
        var hrDept = new Department { Code = "HR", Name = "HR Dept" };
        context.Departments.AddRange(itDept, hrDept);
        await context.SaveChangesAsync();

        // Trưởng phòng IT
        var managerEmp = new Employee { EmployeeCode = "MNG01", FullName = "IT Manager", DepartmentId = itDept.Id };
        // Nhân viên phòng HR
        var applicantEmp = new Employee { EmployeeCode = "EMP02", FullName = "HR Staff", DepartmentId = hrDept.Id };
        context.Employees.AddRange(managerEmp, applicantEmp);
        await context.SaveChangesAsync();

        var managerUser = new User
        {
            Username = "it_manager",
            PasswordHash = "hash",
            Role = UserRole.Manager,
            EmployeeId = managerEmp.Id,
            IsActive = true
        };
        context.Users.Add(managerUser);

        var leave = new LeaveRequest
        {
            EmployeeId = applicantEmp.Id,
            FromDate = new DateOnly(2026, 9, 1),
            ToDate = new DateOnly(2026, 9, 2),
            Status = RequestStatus.Pending,
            Reason = "Việc riêng"
        };
        context.LeaveRequests.Add(leave);
        await context.SaveChangesAsync();

        var approvalService = new ApprovalService(context);
        var dto = new ApprovalActionDto
        {
            ApproverId = managerEmp.Id,
            Action = "APPROVE"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => approvalService.ApproveOrRejectLeaveRequestAsync(leave.Id, dto));
        Assert.Contains("Trưởng phòng chỉ được phép phê duyệt đơn của nhân viên thuộc phòng ban mình quản lý", ex.Message);
    }

    [Fact]
    public async Task Manager_ApproveRequest_SameDepartment_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var itDept = new Department { Code = "IT", Name = "IT Dept" };
        context.Departments.Add(itDept);
        await context.SaveChangesAsync();

        // Trưởng phòng và Nhân viên cùng phòng IT
        var managerEmp = new Employee { EmployeeCode = "MNG01", FullName = "IT Manager", DepartmentId = itDept.Id };
        var applicantEmp = new Employee { EmployeeCode = "EMP01", FullName = "IT Staff", DepartmentId = itDept.Id };
        context.Employees.AddRange(managerEmp, applicantEmp);
        await context.SaveChangesAsync();

        var managerUser = new User
        {
            Username = "it_manager",
            PasswordHash = "hash",
            Role = UserRole.Manager,
            EmployeeId = managerEmp.Id,
            IsActive = true
        };
        context.Users.Add(managerUser);

        var leave = new LeaveRequest
        {
            EmployeeId = applicantEmp.Id,
            FromDate = new DateOnly(2026, 9, 1),
            ToDate = new DateOnly(2026, 9, 2),
            Status = RequestStatus.Pending,
            Reason = "Nghỉ ốm"
        };
        context.LeaveRequests.Add(leave);
        await context.SaveChangesAsync();

        var approvalService = new ApprovalService(context);
        var dto = new ApprovalActionDto
        {
            ApproverId = managerEmp.Id,
            Action = "APPROVE"
        };

        // Act
        var result = await approvalService.ApproveOrRejectLeaveRequestAsync(leave.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("APPROVED", result.Status);
    }
}