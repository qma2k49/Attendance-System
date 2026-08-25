using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.AttendanceLogs;

public class BatchIngestAttendanceLogDto
{
    [Required(ErrorMessage = "Danh sách logs không được để trống")]
    [MinLength(1, ErrorMessage = "Danh sách logs phải có ít nhất 1 bản ghi")]
    public List<IngestAttendanceLogDto> Logs { get; set; } = new();
}