using AttendanceApi.Domain.Enums;

namespace AttendanceApi.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string? Position { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 1 Employee -> N DeviceEmployeeMappings
    public ICollection<DeviceEmployeeMapping> DeviceEmployeeMappings { get; set; } = new List<DeviceEmployeeMapping>();
}