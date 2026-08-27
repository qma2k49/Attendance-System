namespace AttendanceApi.Domain.Enums;

public enum DailyAttendanceStatus
{
    Absent = 0,
    Present = 1,
    Late = 2,
    EarlyLeave = 3,
    LateAndEarlyLeave = 4,
    Holiday = 5,
    Off = 6
}
