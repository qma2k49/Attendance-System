using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.Employees;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

public class EmployeeService : IEmployeeService
{
    private readonly AttendanceDbContext _context;

    public EmployeeService(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<EmployeeResponseDto>> GetPagedAsync(EmployeeFilterDto filter)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .AsQueryable();

        // 1. Lọc theo từ khóa (Mã NV, Tên)
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim().ToLower();
            query = query.Where(e => e.EmployeeCode.ToLower().Contains(keyword) || 
                                     e.FullName.ToLower().Contains(keyword));
        }

        // 2. Lọc theo phòng ban
        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);
        }

        // 3. Lọc theo trạng thái
        if (filter.Status.HasValue)
        {
            query = query.Where(e => e.Status == filter.Status.Value);
        }

        // 4. Đếm tổng số bản ghi trước khi phân trang
        var totalItems = await query.CountAsync();

        // 5. Chuẩn hóa phân trang (tránh số âm)
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeResponseDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                Position = e.Position,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Status = e.Status.ToString().ToUpper(),
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();

        return new PagedResultDto<EmployeeResponseDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<EmployeeResponseDto?> GetByIdAsync(int id)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Where(e => e.Id == id)
            .Select(e => new EmployeeResponseDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                Position = e.Position,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Status = e.Status.ToString().ToUpper(),
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto dto)
    {
        var normalizedCode = dto.EmployeeCode.Trim().ToUpper();

        // Kiểm tra trùng mã nhân viên
        var isCodeExisted = await _context.Employees.AnyAsync(e => e.EmployeeCode == normalizedCode);
        if (isCodeExisted)
        {
            throw new InvalidOperationException($"Mã nhân viên '{normalizedCode}' đã tồn tại trong hệ thống.");
        }

        // Kiểm tra tính tồn tại của phòng ban nếu có truyền DepartmentId
        string? departmentName = null;
        if (dto.DepartmentId.HasValue)
        {
            var dept = await _context.Departments.FindAsync(dto.DepartmentId.Value);
            if (dept == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy phòng ban có ID {dto.DepartmentId.Value}.");
            }
            departmentName = dept.Name;
        }

        var employee = new Employee
        {
            EmployeeCode = normalizedCode,
            FullName = dto.FullName.Trim(),
            DepartmentId = dto.DepartmentId,
            Position = dto.Position?.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = EmployeeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return new EmployeeResponseDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FullName = employee.FullName,
            DepartmentId = employee.DepartmentId,
            DepartmentName = departmentName,
            Position = employee.Position,
            StartDate = employee.StartDate,
            EndDate = employee.EndDate,
            Status = employee.Status.ToString().ToUpper(),
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<EmployeeResponseDto?> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return null;
        }

        // Kiểm tra phòng ban hợp lệ nếu thay đổi DepartmentId
        if (dto.DepartmentId.HasValue && dto.DepartmentId != employee.DepartmentId)
        {
            var deptExisted = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId.Value);
            if (!deptExisted)
            {
                throw new KeyNotFoundException($"Không tìm thấy phòng ban có ID {dto.DepartmentId.Value}.");
            }
        }

        // Kiểm tra tính hợp lệ của ngày kết thúc nếu nhân viên cập nhật EndDate
        if (dto.EndDate.HasValue && dto.EndDate.Value < employee.StartDate)
        {
            throw new ArgumentException("Ngày kết thúc (EndDate) không thể nhỏ hơn ngày bắt đầu làm việc (StartDate).");
        }

        employee.FullName = dto.FullName.Trim();
        employee.DepartmentId = dto.DepartmentId;
        employee.Position = dto.Position?.Trim();
        employee.Status = dto.Status;
        employee.EndDate = dto.EndDate;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Tải lại thông tin phòng ban mới nếu có cập nhật
        var deptName = employee.DepartmentId.HasValue 
            ? (await _context.Departments.Where(d => d.Id == employee.DepartmentId.Value).Select(d => d.Name).FirstOrDefaultAsync())
            : null;

        return new EmployeeResponseDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FullName = employee.FullName,
            DepartmentId = employee.DepartmentId,
            DepartmentName = deptName,
            Position = employee.Position,
            StartDate = employee.StartDate,
            EndDate = employee.EndDate,
            Status = employee.Status.ToString().ToUpper(),
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return false;
        }

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
        return true;
    }
}