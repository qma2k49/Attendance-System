using System.ComponentModel.DataAnnotations;
using AttendanceApi.Domain.Enums;

namespace AttendanceApi.DTOs.AttendanceLogs;

public class IngestAttendanceLogDto
{
    [Required(ErrorMessage = "DeviceCode không được để trống")]
    [MaxLength(50, ErrorMessage = "DeviceCode không vượt quá 50 ký tự")]
    public string DeviceCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "DeviceUserId không được để trống")]
    [MaxLength(50, ErrorMessage = "DeviceUserId không vượt quá 50 ký tự")]
    public string DeviceUserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "CheckTime không được để trống")]
    public DateTime CheckTime { get; set; }

    public VerifyModeEnum VerifyMode { get; set; } = VerifyModeEnum.Fingerprint;

    public string? RawPayload { get; set; }
}