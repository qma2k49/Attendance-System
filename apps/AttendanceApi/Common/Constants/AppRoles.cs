namespace AttendanceApi.Common.Constants;

public static class AppRoles
{
    public const string Admin = "ADMIN";
    public const string HRManager = "HRMANAGER";
    public const string Manager = "MANAGER";
    public const string Employee = "EMPLOYEE";

    public const string AdminOrHR = $"{Admin},{HRManager}";
    public const string Approvers = $"{Admin},{HRManager},{Manager}";
}