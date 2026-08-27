using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceProcessing;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AttendanceApi.Services;

public class AttendanceProcessingEngine : IAttendanceProcessingEngine
{
    private readonly AttendanceDbContext _context;
    private readonly ILogger<AttendanceProcessingEngine> _logger;

    public AttendanceProcessingEngine(AttendanceDbContext context, ILogger<AttendanceProcessingEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProcessAttendanceResultDto> ProcessDailyAttendanceAsync(
        ProcessAttendanceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var workDate = request.WorkDate;
        var startOfDay = workDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = workDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var result = new ProcessAttendanceResultDto
        {
            WorkDate = workDate,
            SuccessCount = 0,
            ErrorCount = 0
        };

        // 1. Quét danh sách phân ca trong ngày (kèm Shift và Employee)
        var scheduleQuery = _context.WorkSchedules
            .Include(s => s.WorkShift)
            .Include(s => s.Employee)
            .Where(s => s.WorkDate == workDate);

        if (request.EmployeeId.HasValue)
        {
            scheduleQuery = scheduleQuery.Where(s => s.EmployeeId == request.EmployeeId.Value);
        }

        var schedules = await scheduleQuery.ToListAsync(cancellationToken);
        result.TotalEmployeesProcessed = schedules.Count;

        if (schedules.Count == 0)
        {
            result.Message = $"Không tìm thấy lịch làm việc nào trong ngày {workDate:yyyy-MM-dd}.";
            return result;
        }

        // 2. Lấy danh sách ánh xạ thiết bị của nhân viên
        var employeeIds = schedules.Select(s => s.EmployeeId).Distinct().ToList();
        var deviceMappings = await _context.DeviceEmployeeMappings
            .Where(m => employeeIds.Contains(m.EmployeeId))
            .ToListAsync(cancellationToken);

        var deviceUserIdToEmployeeMap = deviceMappings
            .GroupBy(m => m.DeviceUserId)
            .ToDictionary(g => g.Key, g => g.First().EmployeeId);

        var deviceUserIds = deviceMappings.Select(m => m.DeviceUserId).Distinct().ToList();

        // 3. Lấy tất cả raw logs trong ngày của các DeviceUserId liên quan
        var rawLogs = await _context.RawAttendanceLogs
            .Where(l => deviceUserIds.Contains(l.DeviceUserId) &&
                        l.CheckTime >= startOfDay &&
                        l.CheckTime <= endOfDay)
            .OrderBy(l => l.CheckTime)
            .ToListAsync(cancellationToken);

        result.TotalRawLogsProcessed = rawLogs.Count;

        // 4. Lấy các bản ghi DailyAttendanceRecord đã tồn tại để thực hiện Upsert
        var existingRecords = await _context.DailyAttendanceRecords
            .Where(r => r.WorkDate == workDate && employeeIds.Contains(r.EmployeeId))
            .ToDictionaryAsync(r => r.EmployeeId, cancellationToken);

        var logsToUpdate = new List<RawAttendanceLog>();

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var schedule in schedules)
            {
                var employeeId = schedule.EmployeeId;
                var shift = schedule.WorkShift;

                if (shift == null)
                {
                    _logger.LogWarning("Lịch làm việc ID {ScheduleId} của nhân viên {EmployeeId} không có ca làm việc hợp lệ.", schedule.Id, employeeId);
                    result.ErrorCount++;
                    continue;
                }

                // Lấy tất cả device_user_id của nhân viên này
                var userDeviceIds = deviceMappings
                    .Where(m => m.EmployeeId == employeeId)
                    .Select(m => m.DeviceUserId)
                    .ToHashSet();

                // Lọc các lượt quẹt của nhân viên trong ngày
                var empLogs = rawLogs
                    .Where(l => userDeviceIds.Contains(l.DeviceUserId))
                    .OrderBy(l => l.CheckTime)
                    .ToList();

                DateTime? checkIn = null;
                DateTime? checkOut = null;
                int lateMinutes = 0;
                int earlyMinutes = 0;
                decimal workHours = 0m;
                DailyAttendanceStatus status;

                if (empLogs.Count == 0)
                {
                    // Trường hợp vắng mặt
                    status = DailyAttendanceStatus.Absent;
                }
                else
                {
                    // Xác định Check-In và Check-Out
                    checkIn = empLogs.First().CheckTime;
                    checkOut = empLogs.Count > 1 ? empLogs.Last().CheckTime : checkIn;

                    var shiftStart = workDate.ToDateTime(shift.StartTime, DateTimeKind.Utc);
                    var shiftEnd = workDate.ToDateTime(shift.EndTime, DateTimeKind.Utc);

                    // Xử lý ca qua đêm (End time < Start time)
                    if (shift.EndTime < shift.StartTime)
                    {
                        shiftEnd = shiftEnd.AddDays(1);
                    }

                    // Tính phút đi muộn (Vượt quá GracePeriodMinutes)
                    var graceStartTime = shiftStart.AddMinutes(shift.GracePeriodMinutes);
                    if (checkIn.Value > graceStartTime)
                    {
                        lateMinutes = (int)Math.Max(0, (checkIn.Value - shiftStart).TotalMinutes);
                    }

                    // Tính phút về sớm
                    if (checkOut.HasValue && checkOut.Value < shiftEnd)
                    {
                        earlyMinutes = (int)Math.Max(0, (shiftEnd - checkOut.Value).TotalMinutes);
                    }

                    // Tính tổng giờ công thực tế
                    var totalWorkTime = (checkOut.Value - checkIn.Value);
                    var rawWorkHours = (decimal)totalWorkTime.TotalHours;

                    // Khấu trừ thời gian nghỉ giữa ca (nếu có cấu hình)
                    if (shift.BreakStartTime.HasValue && shift.BreakEndTime.HasValue)
                    {
                        var breakDuration = (decimal)(shift.BreakEndTime.Value - shift.BreakStartTime.Value).TotalHours;
                        if (rawWorkHours > breakDuration)
                        {
                            rawWorkHours -= breakDuration;
                        }
                    }

                    workHours = Math.Max(0, Math.Round(rawWorkHours, 2));

                    // Xác định trạng thái DailyAttendanceStatus
                    var isLate = lateMinutes > 0;
                    var isEarly = earlyMinutes > 0;

                    if (isLate && isEarly)
                    {
                        status = DailyAttendanceStatus.LateAndEarlyLeave;
                    }
                    else if (isLate)
                    {
                        status = DailyAttendanceStatus.Late;
                    }
                    else if (isEarly)
                    {
                        status = DailyAttendanceStatus.EarlyLeave;
                    }
                    else
                    {
                        status = DailyAttendanceStatus.Present;
                    }

                    // Đánh dấu log thô đã xử lý
                    foreach (var log in empLogs)
                    {
                        if (log.ProcessedStatus != ProcessedStatus.Processed)
                        {
                            log.ProcessedStatus = ProcessedStatus.Processed;
                            logsToUpdate.Add(log);
                        }
                    }
                }

                // Upsert vào DailyAttendanceRecords
                if (existingRecords.TryGetValue(employeeId, out var existingRecord))
                {
                    existingRecord.WorkShiftId = shift.Id;
                    existingRecord.CheckInTime = checkIn;
                    existingRecord.CheckOutTime = checkOut;
                    existingRecord.LateMinutes = lateMinutes;
                    existingRecord.EarlyMinutes = earlyMinutes;
                    existingRecord.WorkHours = workHours;
                    existingRecord.Status = status;
                    existingRecord.UpdatedAt = DateTime.UtcNow;
                    _context.DailyAttendanceRecords.Update(existingRecord);
                }
                else
                {
                    var newRecord = new DailyAttendanceRecord
                    {
                        EmployeeId = employeeId,
                        WorkDate = workDate,
                        WorkShiftId = shift.Id,
                        CheckInTime = checkIn,
                        CheckOutTime = checkOut,
                        LateMinutes = lateMinutes,
                        EarlyMinutes = earlyMinutes,
                        WorkHours = workHours,
                        Status = status,
                        ProcessedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow

                    };
                    await _context.DailyAttendanceRecords.AddAsync(newRecord, cancellationToken);
                }

                result.SuccessCount++;
            }

            // Lưu thay đổi cập nhật log thô và bảng công
            if (logsToUpdate.Count > 0)
            {
                _context.RawAttendanceLogs.UpdateRange(logsToUpdate);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            result.Message = $"Đã xử lý thành công {result.SuccessCount}/{result.TotalEmployeesProcessed} nhân viên.";
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Lỗi xảy ra khi xử lý công ngày {WorkDate}", workDate);
            throw;
        }
    }
}