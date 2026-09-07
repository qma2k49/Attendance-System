using AttendanceApi.Domain.Entities;
using AttendanceApi.DTOs.Departments;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AttendanceApi.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AttendanceDbContext _context;
    private readonly IMemoryCache? _cache;
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(10);

    private static readonly HashSet<string> DepartmentCacheKeys = new();
    private static readonly object LockObj = new();

    public DepartmentService(AttendanceDbContext context, IMemoryCache? cache = null)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IEnumerable<DepartmentResponseDto>> GetAllAsync(string? keyword = null)
    {
        var cacheKey = $"departments:all:{keyword?.Trim().ToLowerInvariant() ?? "none"}";

        if (_cache != null && _cache.TryGetValue(cacheKey, out IEnumerable<DepartmentResponseDto>? cachedList) && cachedList != null)
        {
            return cachedList;
        }

        var query = _context.Departments
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = keyword.Trim().ToLower();
            query = query.Where(d => d.Code.ToLower().Contains(search) || d.Name.ToLower().Contains(search));
        }

        var result = await query
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

        if (_cache != null)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(AbsoluteExpiration)
                .SetSlidingExpiration(SlidingExpiration);

            _cache.Set(cacheKey, result, options);
            TrackCacheKey(cacheKey);
        }

        return result;
    }

    public async Task<DepartmentResponseDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"departments:id:{id}";

        if (_cache != null && _cache.TryGetValue(cacheKey, out DepartmentResponseDto? cachedDept) && cachedDept != null)
        {
            return cachedDept;
        }

        var result = await _context.Departments
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

        if (result != null && _cache != null)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(AbsoluteExpiration)
                .SetSlidingExpiration(SlidingExpiration);

            _cache.Set(cacheKey, result, options);
            TrackCacheKey(cacheKey);
        }

        return result;
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

        InvalidateCache();

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

        InvalidateCache();

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

        InvalidateCache();
        return true;
    }

    private static void TrackCacheKey(string key)
    {
        lock (LockObj)
        {
            DepartmentCacheKeys.Add(key);
        }
    }

    private void InvalidateCache()
    {
        if (_cache == null) return;

        lock (LockObj)
        {
            foreach (var key in DepartmentCacheKeys)
            {
                _cache.Remove(key);
            }
            DepartmentCacheKeys.Clear();
        }
    }
}