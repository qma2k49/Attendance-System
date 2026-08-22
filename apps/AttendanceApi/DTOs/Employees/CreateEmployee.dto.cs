using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.Employees;

public class CreateEmployeeDto : IValidatableObject
{
    [Required(ErrorMessage = "Mã nhân viên không được để trống")]
    [MaxLength(50, ErrorMessage = "Mã nhân viên không vượt quá 50 ký tự")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [MaxLength(255, ErrorMessage = "Họ và tên không vượt quá 255 ký tự")]
    public string FullName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    [MaxLength(100, ErrorMessage = "Vị trí chức vụ không vượt quá 100 ký tự")]
    public string? Position { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu làm việc không được để trống")]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate.HasValue && EndDate.Value < StartDate)
        {
            yield return new ValidationResult(
                "Ngày kết thúc (EndDate) phải lớn hơn hoặc bằng ngày bắt đầu (StartDate).",
                new[] { nameof(EndDate) }
            );
        }
    }
}