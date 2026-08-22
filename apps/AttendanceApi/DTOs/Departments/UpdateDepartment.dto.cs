using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.Departments;

public class UpdateDepartmentDto
{
    [Required(ErrorMessage = "Tên phòng ban không được để trống")]
    [MaxLength(255, ErrorMessage = "Tên phòng ban không vượt quá 255 ký tự")]
    public string Name { get; set; } = string.Empty;
}