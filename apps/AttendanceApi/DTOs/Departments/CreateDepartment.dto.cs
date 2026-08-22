using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.Departments;

public class CreateDepartmentDto
{
    [Required(ErrorMessage = "Mã phòng ban không được để trống")]
    [MaxLength(50, ErrorMessage = "Mã phòng ban không vượt quá 50 ký tự")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên phòng ban không được để trống")]
    [MaxLength(255, ErrorMessage = "Tên phòng ban không vượt quá 255 ký tự")]
    public string Name { get; set; } = string.Empty;
}