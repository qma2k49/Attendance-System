using AttendanceApi.Domain.Entities;
using AttendanceApi.DTOs.Departments;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AttendanceDbContext _context;

    public DepartmentService(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DepartmentResponseDto>> GetAllAsync(string? keyword)
    {
        var query = _context.Departments
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = keyword.Trim().ToLower();
            query = query.Where(d => d.Code.ToLower().Contains(search) || d.Name.ToLower().Contains(search));
        }

        return await query
            .OrderBy(d => d.Id)
            .Select(d => new DepartmentResponseDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                EmployeeCount = d.Employees.Count(),
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<DepartmentResponseDto?> GetByIdAsync(int id)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentResponseDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                EmployeeCount = d.Employees.Count(),
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto)
    {
        var normalizedCode = dto.Code.Trim().ToUpper();
        
        var isExisted = await _context.Departments.AnyAsync(d => d.Code == normalizedCode);
        if (isExisted)
        {
            throw new InvalidOperationException($"Mã phòng ban '{normalizedCode}' đã tồn tại trong hệ thống.");
        }

        var department = new Department
        {
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return new DepartmentResponseDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            EmployeeCount = 0,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        };
    }

    public async Task<DepartmentResponseDto?> UpdateAsync(int id, UpdateDepartmentDto dto)
    {
        var department = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return null;
        }

        department.Name = dto.Name.Trim();
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DepartmentResponseDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            EmployeeCount = department.Employees.Count,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
        {
            return false;
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
        return true;
    }
}