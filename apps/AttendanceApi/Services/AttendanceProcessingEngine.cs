using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.AttendanceProcessing;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AttendanceApi.Services;

public class AttendanceProcessingEngine : IAttendanceProcessingEngine
{
    private const int BufferMinutes = 2;              // Khoảng cách tối thiểu giữa 2 lần quẹt hợp lệ
    private const int SearchWindowHours = 2;          // Cửa sổ mở rộng quét log trước/sau ca làm việc
    private const int SinglePunchThresholdHours = 3;  // Ngưỡng phân loại Check-In hay Check-Out khi chỉ có 1 lần quẹt

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

        var deviceUserIds = deviceMappings.Select(m => m.DeviceUserId).Distinct().ToList();

        // 3. Tra cứu Đơn nghỉ phép & Đơn bổ sung công ĐÃ DUYỆT (APPROVED)
        var approvedLeaveRequests = await _context.LeaveRequests
            .Where(l => employeeIds.Contains(l.EmployeeId) &&
                        l.Status == RequestStatus.Approved &&
                        l.FromDate <= workDate &&
                        l.ToDate >= workDate)
            .ToListAsync(cancellationToken);

        var approvedAdjustments = await _context.AttendanceAdjustments
            .Where(a => employeeIds.Contains(a.EmployeeId) &&
                        a.Status == RequestStatus.Approved &&
                        a.WorkDate == workDate)
            .ToListAsync(cancellationToken);

        // 4. Xác định khung thời gian quét log tổng thể (Bao phủ cả ca thường lẫn ca qua đêm)
        var minGlobalScanTime = workDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-SearchWindowHours);
        var maxGlobalScanTime = workDate.AddDays(1).ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc).AddHours(SearchWindowHours);

        var allRawLogs = await _context.RawAttendanceLogs
            .Where(l => deviceUserIds.Contains(l.DeviceUserId) &&
                        l.CheckTime >= minGlobalScanTime &&
                        l.CheckTime <= maxGlobalScanTime)
            .OrderBy(l => l.CheckTime)
            .ToListAsync(cancellationToken);

        result.TotalRawLogsProcessed = allRawLogs.Count;

        // 5. Lấy các bản ghi DailyAttendanceRecord đã tồn tại để thực hiện Upsert
        var existingRecords = await _context.DailyAttendanceRecords
            .Where(r => r.WorkDate == workDate && employeeIds.Contains(r.EmployeeId))
            .ToDictionaryAsync(r => r.EmployeeId, cancellationToken);

        var logsToUpdate = new List<RawAttendanceLog>();

        var isRelational = _context.Database.IsRelational();
        using var transaction = isRelational ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
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

                // Xác định thời điểm bắt đầu và kết thúc ca chuẩn (bao gồm xử lý ca qua đêm)
                var isOvernight = shift.IsOvernight || shift.EndTime < shift.StartTime;
                var shiftStart = workDate.ToDateTime(shift.StartTime, DateTimeKind.Utc);
                var shiftEnd = isOvernight
                    ? workDate.AddDays(1).ToDateTime(shift.EndTime, DateTimeKind.Utc)
                    : workDate.ToDateTime(shift.EndTime, DateTimeKind.Utc);

                // Cửa sổ quét log cho ca làm việc này
                var windowStart = shiftStart.AddHours(-SearchWindowHours);
                var windowEnd = shiftEnd.AddHours(SearchWindowHours);

                // Lấy các log thô rơi vào khung giờ ca của nhân viên
                var employeeShiftLogs = allRawLogs
                    .Where(l => userDeviceIds.Contains(l.DeviceUserId) &&
                                l.CheckTime >= windowStart &&
                                l.CheckTime <= windowEnd)
                    .OrderBy(l => l.CheckTime)
                    .ToList();

                // Lọc trùng quẹt thẻ liên tiếp (Buffer Window Deduplication)
                var deduplicatedLogs = DeduplicateLogs(employeeShiftLogs, TimeSpan.FromMinutes(BufferMinutes));

                DateTime? checkIn = null;
                DateTime? checkOut = null;
                int lateMinutes = 0;
                int earlyMinutes = 0;
                decimal workHours = 0m;
                DailyAttendanceStatus status = DailyAttendanceStatus.Absent;

                // TH1: Kiểm tra xem nhân viên có đơn nghỉ phép đã duyệt hay không
                var approvedLeave = approvedLeaveRequests.FirstOrDefault(l => l.EmployeeId == employeeId);
                if (approvedLeave != null)
                {
                    status = DailyAttendanceStatus.OnLeave;
                    // Phép năm, Thai sản, Hiếu hỷ được tính hưởng lương đủ giờ ca
                    if (approvedLeave.LeaveType == LeaveType.Annual ||
                        approvedLeave.LeaveType == LeaveType.Maternity ||
                        approvedLeave.LeaveType == LeaveType.Compassionate)
                    {
                        workHours = shift.WorkHours;
                    }
                    else
                    {
                        workHours = 0m;
                    }
                    lateMinutes = 0;
                    earlyMinutes = 0;
                }
                else
                {
                    // Lấy đơn giải trình công đã duyệt (nếu có)
                    var approvedAdjustment = approvedAdjustments.FirstOrDefault(a => a.EmployeeId == employeeId);

                    if (deduplicatedLogs.Count == 0)
                    {
                        if (approvedAdjustment != null && (approvedAdjustment.AdjustedCheckIn.HasValue || approvedAdjustment.AdjustedCheckOut.HasValue))
                        {
                            checkIn = approvedAdjustment.AdjustedCheckIn;
                            checkOut = approvedAdjustment.AdjustedCheckOut;
                        }
                        else
                        {
                            status = DailyAttendanceStatus.Absent;
                        }
                    }
                    else if (deduplicatedLogs.Count == 1)
                    {
                        var singlePunch = deduplicatedLogs[0].CheckTime;
                        var distToStart = Math.Abs((singlePunch - shiftStart).TotalHours);
                        var distToEnd = Math.Abs((singlePunch - shiftEnd).TotalHours);

                        if (distToStart <= distToEnd && distToStart <= SinglePunchThresholdHours)
                        {
                            checkIn = singlePunch;
                        }
                        else if (distToEnd <= SinglePunchThresholdHours)
                        {
                            checkOut = singlePunch;
                        }
                        else
                        {
                            checkIn = singlePunch;
                        }

                        // Ghi đè bằng giờ điều chỉnh nếu có đơn giải trình
                        if (approvedAdjustment != null)
                        {
                            if (approvedAdjustment.AdjustedCheckIn.HasValue) checkIn = approvedAdjustment.AdjustedCheckIn.Value;
                            if (approvedAdjustment.AdjustedCheckOut.HasValue) checkOut = approvedAdjustment.AdjustedCheckOut.Value;
                        }
                    }
                    else
                    {
                        checkIn = deduplicatedLogs.First().CheckTime;
                        checkOut = deduplicatedLogs.Last().CheckTime;

                        // Ghi đè bằng giờ điều chỉnh nếu có đơn giải trình
                        if (approvedAdjustment != null)
                        {
                            if (approvedAdjustment.AdjustedCheckIn.HasValue) checkIn = approvedAdjustment.AdjustedCheckIn.Value;
                            if (approvedAdjustment.AdjustedCheckOut.HasValue) checkOut = approvedAdjustment.AdjustedCheckOut.Value;
                        }
                    }

                    // Nếu không phải nghỉ phép, thực hiện tính toán công ngày dựa trên CheckIn và CheckOut
                    if (!checkIn.HasValue && !checkOut.HasValue)
                    {
                        status = DailyAttendanceStatus.Absent;
                        workHours = 0m;
                        lateMinutes = 0;
                        earlyMinutes = 0;
                    }
                    else
                    {
                        // Tính phút đi muộn
                        if (checkIn.HasValue)
                        {
                            var graceStart = shiftStart.AddMinutes(shift.GracePeriodMinutes);
                            if (checkIn.Value > graceStart)
                            {
                                lateMinutes = (int)Math.Max(0, (checkIn.Value - shiftStart).TotalMinutes);
                            }
                        }

                        // Tính phút về sớm
                        if (checkOut.HasValue && checkOut.Value < shiftEnd)
                        {
                            earlyMinutes = (int)Math.Max(0, (shiftEnd - checkOut.Value).TotalMinutes);
                        }

                        // Tính tổng giờ làm việc thực tế
                        if (checkIn.HasValue && checkOut.HasValue)
                        {
                            var totalWorkTime = checkOut.Value - checkIn.Value;
                            var rawWorkHours = (decimal)totalWorkTime.TotalHours;

                            if (shift.BreakStartTime.HasValue && shift.BreakEndTime.HasValue)
                            {
                                var breakStart = workDate.ToDateTime(shift.BreakStartTime.Value, DateTimeKind.Utc);
                                var breakEnd = workDate.ToDateTime(shift.BreakEndTime.Value, DateTimeKind.Utc);

                                if (shift.BreakEndTime.Value < shift.BreakStartTime.Value)
                                {
                                    breakEnd = workDate.AddDays(1).ToDateTime(shift.BreakEndTime.Value, DateTimeKind.Utc);
                                }

                                var breakDuration = (decimal)(breakEnd - breakStart).TotalHours;
                                if (rawWorkHours > breakDuration)
                                {
                                    rawWorkHours -= breakDuration;
                                }
                            }

                            workHours = Math.Max(0, Math.Round(rawWorkHours, 2));
                        }

                        // Xác định trạng thái công
                        var isLate = lateMinutes > 0;
                        var isEarly = earlyMinutes > 0 || !checkOut.HasValue;

                        if (!checkIn.HasValue && checkOut.HasValue)
                        {
                            status = DailyAttendanceStatus.Late;
                        }
                        else if (isLate && isEarly)
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
                    }
                }

                // Đánh dấu các log thô đã xử lý
                foreach (var log in employeeShiftLogs)
                {
                    if (log.ProcessedStatus != ProcessedStatus.Processed)
                    {
                        log.ProcessedStatus = ProcessedStatus.Processed;
                        logsToUpdate.Add(log);
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

            // Cập nhật trạng thái log thô
            if (logsToUpdate.Count > 0)
            {
                _context.RawAttendanceLogs.UpdateRange(logsToUpdate);
            }

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            result.Message = $"Đã xử lý nâng cao thành công {result.SuccessCount}/{result.TotalEmployeesProcessed} nhân viên.";
            return result;
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            _logger.LogError(ex, "Lỗi xảy ra khi xử lý công nâng cao cho ngày {WorkDate}", workDate);
            throw;
        }
    }

    private static List<RawAttendanceLog> DeduplicateLogs(List<RawAttendanceLog> logs, TimeSpan bufferWindow)
    {
        if (logs.Count <= 1)
        {
            return logs;
        }

        var deduplicated = new List<RawAttendanceLog> { logs[0] };
        var lastValidTime = logs[0].CheckTime;

        for (int i = 1; i < logs.Count; i++)
        {
            if (logs[i].CheckTime - lastValidTime >= bufferWindow)
            {
                deduplicated.Add(logs[i]);
                lastValidTime = logs[i].CheckTime;
            }
        }

        return deduplicated;
    }
}