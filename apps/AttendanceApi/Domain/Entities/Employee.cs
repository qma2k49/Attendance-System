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
    public virtual ICollection<DeviceEmployeeMapping> DeviceEmployeeMappings { get; set; } = new List<DeviceEmployeeMapping>();

    // 1 Employee -> N WorkSchedules
    public virtual ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();

    // 1 Employee -> N DailyAttendanceRecords
    public virtual ICollection<DailyAttendanceRecord> DailyAttendanceRecords { get; set; } = new List<DailyAttendanceRecord>();

    // Đơn từ do nhân viên tạo
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public virtual ICollection<AttendanceAdjustment> AttendanceAdjustments { get; set; } = new List<AttendanceAdjustment>();

    // Đơn từ do nhân viên này phê duyệt (Approver)
    public virtual ICollection<LeaveRequest> ApprovedLeaveRequests { get; set; } = new List<LeaveRequest>();
    public virtual ICollection<AttendanceAdjustment> ApprovedAttendanceAdjustments { get; set; } = new List<AttendanceAdjustment>();

    // Bảng công tháng của nhân viên
    public virtual ICollection<MonthlyTimesheetSummary> MonthlyTimesheetSummaries { get; set; } = new List<MonthlyTimesheetSummary>();

    // Bảng công tháng do nhân viên này chốt / khóa
    public virtual ICollection<MonthlyTimesheetSummary> FinalizedMonthlyTimesheets { get; set; } = new List<MonthlyTimesheetSummary>();
}
