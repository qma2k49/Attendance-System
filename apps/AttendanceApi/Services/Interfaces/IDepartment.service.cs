using AttendanceApi.DTOs.Departments;

namespace AttendanceApi.Services;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentResponseDto>> GetAllAsync(string? keyword);
    Task<DepartmentResponseDto?> GetByIdAsync(int id);
    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto);
    Task<DepartmentResponseDto?> UpdateAsync(int id, UpdateDepartmentDto dto);
    Task<bool> DeleteAsync(int id);
}