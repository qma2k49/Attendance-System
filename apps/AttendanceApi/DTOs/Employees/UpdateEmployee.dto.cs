using System.ComponentModel.DataAnnotations;
using AttendanceApi.Domain.Enums;

namespace AttendanceApi.DTOs.Employees;

public class UpdateEmployeeDto : IValidatableObject
{
    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [MaxLength(255, ErrorMessage = "Họ và tên không vượt quá 255 ký tự")]
    public string FullName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    [MaxLength(100, ErrorMessage = "Vị trí chức vụ không vượt quá 100 ký tự")]
    public string? Position { get; set; }

    [Required(ErrorMessage = "Trạng thái nhân viên không được để trống")]
    public EmployeeStatus Status { get; set; }

    public DateOnly? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield break;
    }
}