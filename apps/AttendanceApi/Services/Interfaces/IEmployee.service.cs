using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.Employees;

namespace AttendanceApi.Services;


public interface IEmployeeService
{
    Task<PagedResultDto<EmployeeResponseDto>> GetPagedAsync(EmployeeFilterDto filter);
    Task<EmployeeResponseDto?> GetByIdAsync(int id);
    Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto dto);
    Task<EmployeeResponseDto?> UpdateAsync(int id, UpdateEmployeeDto dto);
    Task<bool> DeleteAsync(int id);
}