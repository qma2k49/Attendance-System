using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.Mappings;

public class CreateDeviceMappingDto
{
    [Required(ErrorMessage = "DeviceId không được để trống")]
    public int DeviceId { get; set; }

    [Required(ErrorMessage = "DeviceUserId không được để trống")]
    [MaxLength(50, ErrorMessage = "DeviceUserId không vượt quá 50 ký tự")]
    public string DeviceUserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "EmployeeId không được để trống")]
    public int EmployeeId { get; set; }
}