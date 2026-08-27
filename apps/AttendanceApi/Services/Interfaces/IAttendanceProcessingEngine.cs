using AttendanceApi.DTOs.AttendanceProcessing;

namespace AttendanceApi.Services;



public interface IAttendanceProcessingEngine
{
    /// <summary>
    /// Xử lý và tổng hợp dữ liệu quẹt thẻ thô thành bảng công ngày cho một ngày làm việc cụ thể.
    /// </summary>
    /// <param name="request">Tham số ngày làm việc và nhân viên (nếu có)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Kết quả xử lý công ngày</returns>
    Task<ProcessAttendanceResultDto> ProcessDailyAttendanceAsync(
        ProcessAttendanceRequestDto request, 
        CancellationToken cancellationToken = default);
}