namespace AttendanceApi.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    int? EmployeeId { get; }
    string? Role { get; }
    bool IsInRole(string role);
}