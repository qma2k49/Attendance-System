using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.MonthlyTimesheet;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AttendanceApi.Services;

public class TimesheetAggregationService : ITimesheetAggregationService
{
    private readonly AttendanceDbContext _context;
    private readonly ILogger<TimesheetAggregationService> _logger;

    public TimesheetAggregationService(
        AttendanceDbContext context,
        ILogger<TimesheetAggregationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AggregateTimesheetResultDto> AggregateMonthlyTimesheetsAsync(
        AggregateTimesheetRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var year = request.Year;
        var month = request.Month;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var startDate = new DateOnly(year, month, 1);
        var endDate = new DateOnly(year, month, daysInMonth);

        var result = new AggregateTimesheetResultDto
        {
            Year = year,
            Month = month,
            SuccessCount = 0,
            ErrorCount = 0,
            SkippedLockedCount = 0
        };

        // 1. Lọc danh sách nhân viên cần tổng hợp công
        var employeeQuery = _context.Employees.AsNoTracking().AsQueryable();

        if (request.EmployeeId.HasValue)
        {
            employeeQuery = employeeQuery.Where(e => e.Id == request.EmployeeId.Value);
        }

        if (request.DepartmentId.HasValue)
        {
            employeeQuery = employeeQuery.Where(e => e.DepartmentId == request.DepartmentId.Value);
        }

        var employees = await employeeQuery.ToListAsync(cancellationToken);
        result.TotalEmployeesProcessed = employees.Count;

        if (employees.Count == 0)
        {
            result.Message = $"Không tìm thấy nhân viên nào phù hợp để tính công tháng {month:D2}/{year}.";
            return result;
        }

        var employeeIds = employees.Select(e => e.Id).ToList();

        // 2. Tải trước toàn bộ dữ liệu công ngày trong tháng của các nhân viên
        var dailyRecords = await _context.DailyAttendanceRecords
            .AsNoTracking()
            .Where(r => employeeIds.Contains(r.EmployeeId) &&
                        r.WorkDate >= startDate &&
                        r.WorkDate <= endDate)
            .ToListAsync(cancellationToken);

        var dailyRecordsByEmployee = dailyRecords
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 3. Tải trước các đơn nghỉ phép đã APPROVED trong tháng để phân loại phép có lương / không lương
        var approvedLeaveRequests = await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => employeeIds.Contains(l.EmployeeId) &&
                        l.Status == RequestStatus.Approved &&
                        l.FromDate <= endDate &&
                        l.ToDate >= startDate)
            .ToListAsync(cancellationToken);

        var leavesByEmployee = approvedLeaveRequests
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 4. Lấy các bản ghi MonthlyTimesheetSummary đã tồn tại để thực hiện Upsert
        var existingSummaries = await _context.MonthlyTimesheetSummaries
            .Where(m => m.Year == year && m.Month == month && employeeIds.Contains(m.EmployeeId))
            .ToDictionaryAsync(m => m.EmployeeId, cancellationToken);

        var newSummaries = new List<MonthlyTimesheetSummary>();

        foreach (var employee in employees)
        {
            try
            {
                var empId = employee.Id;

                // Kiểm tra nếu bản ghi công tháng đã bị KHÓA (LOCKED) -> Không ghi đè
                if (existingSummaries.TryGetValue(empId, out var existingSummary) &&
                    existingSummary.Status == TimesheetStatus.Locked)
                {
                    _logger.LogInformation("Bỏ qua tính công tháng {Month}/{Year} cho nhân viên ID {EmpId} do bảng công đã bị KHÓA (LOCKED).", month, year, empId);
                    result.SkippedLockedCount++;
                    continue;
                }

                dailyRecordsByEmployee.TryGetValue(empId, out var empDailyRecords);
                empDailyRecords ??= new List<DailyAttendanceRecord>();

                leavesByEmployee.TryGetValue(empId, out var empLeaves);
                empLeaves ??= new List<LeaveRequest>();

                // Tính toán các chỉ số
                decimal actualWorkingDays = 0.0m;
                decimal actualWorkingHours = 0.00m;
                decimal paidLeaveDays = 0.0m;
                decimal unpaidLeaveDays = 0.0m;
                decimal absentDays = 0.0m;
                int lateMinutes = 0;
                int earlyMinutes = 0;
                int lateOccurrences = 0;
                int earlyOccurrences = 0;
                decimal overtimeHours = 0.00m;

                foreach (var record in empDailyRecords)
                {
                    // Ngày đi làm thực tế
                    if (record.Status == DailyAttendanceStatus.Present ||
                        record.Status == DailyAttendanceStatus.Late ||
                        record.Status == DailyAttendanceStatus.EarlyLeave ||
                        record.Status == DailyAttendanceStatus.LateAndEarlyLeave)
                    {
                        actualWorkingDays += 1.0m;
                    }
                    else if (record.Status == DailyAttendanceStatus.Absent)
                    {
                        absentDays += 1.0m;
                    }

                    actualWorkingHours += record.WorkHours;
                    overtimeHours += record.OvertimeHours;
                    lateMinutes += record.LateMinutes;
                    earlyMinutes += record.EarlyMinutes;

                    if (record.LateMinutes > 0) lateOccurrences++;
                    if (record.EarlyMinutes > 0) earlyOccurrences++;
                }

                // Tính ngày nghỉ phép từ các bản ghi DailyAttendanceRecord có trạng thái OnLeave
                var onLeaveRecords = empDailyRecords.Where(r => r.Status == DailyAttendanceStatus.OnLeave).ToList();
                foreach (var leaveRecord in onLeaveRecords)
                {
                    var matchedLeave = empLeaves.FirstOrDefault(l => l.FromDate <= leaveRecord.WorkDate && l.ToDate >= leaveRecord.WorkDate);
                    if (matchedLeave != null)
                    {
                        if (matchedLeave.LeaveType == LeaveType.Annual ||
                            matchedLeave.LeaveType == LeaveType.Maternity ||
                            matchedLeave.LeaveType == LeaveType.Compassionate)
                        {
                            paidLeaveDays += 1.0m;
                        }
                        else
                        {
                            unpaidLeaveDays += 1.0m;
                        }
                    }
                    else
                    {
                        paidLeaveDays += 1.0m; // Mặc định nếu có OnLeave
                    }
                }

                // Tổng số công tính lương
                var totalPayableDays = actualWorkingDays + paidLeaveDays;

                if (existingSummary != null)
                {
                    // Cập nhật bản ghi DRAFT / FINALIZED
                    existingSummary.StandardWorkingDays = request.StandardWorkingDays;
                    existingSummary.ActualWorkingDays = actualWorkingDays;
                    existingSummary.ActualWorkingHours = Math.Round(actualWorkingHours, 2);
                    existingSummary.PaidLeaveDays = paidLeaveDays;
                    existingSummary.UnpaidLeaveDays = unpaidLeaveDays;
                    existingSummary.AbsentDays = absentDays;
                    existingSummary.LateMinutes = lateMinutes;
                    existingSummary.EarlyMinutes = earlyMinutes;
                    existingSummary.LateOccurrences = lateOccurrences;
                    existingSummary.EarlyOccurrences = earlyOccurrences;
                    existingSummary.OvertimeHours = Math.Round(overtimeHours, 2);
                    existingSummary.TotalPayableDays = totalPayableDays;
                    existingSummary.UpdatedAt = DateTime.UtcNow;

                    _context.MonthlyTimesheetSummaries.Update(existingSummary);
                }
                else
                {
                    // Tạo mới bản ghi công tháng
                    var newSummary = new MonthlyTimesheetSummary
                    {
                        EmployeeId = empId,
                        Year = year,
                        Month = month,
                        StandardWorkingDays = request.StandardWorkingDays,
                        ActualWorkingDays = actualWorkingDays,
                        ActualWorkingHours = Math.Round(actualWorkingHours, 2),
                        PaidLeaveDays = paidLeaveDays,
                        UnpaidLeaveDays = unpaidLeaveDays,
                        AbsentDays = absentDays,
                        LateMinutes = lateMinutes,
                        EarlyMinutes = earlyMinutes,
                        LateOccurrences = lateOccurrences,
                        EarlyOccurrences = earlyOccurrences,
                        OvertimeHours = Math.Round(overtimeHours, 2),
                        TotalPayableDays = totalPayableDays,
                        Status = TimesheetStatus.Draft,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    newSummaries.Add(newSummary);
                }

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi tính công tháng cho nhân viên ID {EmpId}", employee.Id);
                result.ErrorCount++;
            }
        }

        if (newSummaries.Count > 0)
        {
            await _context.MonthlyTimesheetSummaries.AddRangeAsync(newSummaries, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        result.Message = $"Tổng hợp công tháng {month:D2}/{year} hoàn tất: {result.SuccessCount} thành công, {result.SkippedLockedCount} bỏ qua (đã khóa), {result.ErrorCount} lỗi.";
        return result;
    }
}