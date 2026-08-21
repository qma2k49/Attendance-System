namespace AttendanceApi.Domain.Entities;

public class DeviceEmployeeMapping
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public AttendanceDevice? AttendanceDevice { get; set; }
    public string DeviceUserId { get; set; } = string.Empty;

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}