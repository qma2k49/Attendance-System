using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.DTOs.AttendanceProcessing;

public class ProcessAttendanceRequestDto
{
    [Required]
    public DateOnly WorkDate { get; set; }

    public int? EmployeeId { get; set; }
}