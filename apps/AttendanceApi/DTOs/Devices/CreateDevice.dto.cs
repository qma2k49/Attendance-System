using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.Devices;

public class CreateDeviceDto
{
    [Required(ErrorMessage = "Mã thiết bị không được để trống")]
    [MaxLength(50, ErrorMessage = "Mã thiết bị không vượt quá 50 ký tự")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên thiết bị không được để trống")]
    [MaxLength(255, ErrorMessage = "Tên thiết bị không vượt quá 255 ký tự")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ IP không được để trống")]
    [MaxLength(45, ErrorMessage = "Địa chỉ IP không vượt quá 45 ký tự")]
    public string IpAddress { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Port phải nằm trong khoảng 1 - 65535")]
    public int Port { get; set; } = 4370;

    [MaxLength(100, ErrorMessage = "Model không vượt quá 100 ký tự")]
    public string? Model { get; set; }

    [MaxLength(100, ErrorMessage = "Serial Number không vượt quá 100 ký tự")]
    public string? SerialNumber { get; set; }

    [MaxLength(255, ErrorMessage = "Vị trí lắp đặt không vượt quá 255 ký tự")]
    public string? Location { get; set; }
}