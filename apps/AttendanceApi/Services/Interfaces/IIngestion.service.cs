using AttendanceApi.DTOs.AttendanceLogs;

namespace AttendanceApi.Services;


public interface IIngestionService
{
    Task<IngestResponseDto> IngestLogsAsync(IEnumerable<IngestAttendanceLogDto> logDtos);
}