using AttendanceApi.Domain.Entities;
using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.Mappings;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace AttendanceApi.Services;

public class DeviceEmployeeMappingService : IDeviceEmployeeMappingService
{
    private readonly AttendanceDbContext _context;

    public DeviceEmployeeMappingService(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<DeviceMappingResponseDto>> GetPagedAsync(DeviceMappingFilterDto filter)
    {
        var query = _context.DeviceEmployeeMappings
            .AsNoTracking()
            .Include(m => m.AttendanceDevice)
            .Include(m => m.Employee)
            .AsQueryable();

        if (filter.DeviceId.HasValue)
        {
            query = query.Where(m => m.DeviceId == filter.DeviceId.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(m => m.EmployeeId == filter.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim().ToLower();
            query = query.Where(m => m.DeviceUserId.ToLower().Contains(keyword) ||
                                     m.Employee!.FullName.ToLower().Contains(keyword) ||
                                     m.Employee!.EmployeeCode.ToLower().Contains(keyword));
        }

        var totalItems = await query.CountAsync();
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new DeviceMappingResponseDto
            {
                Id = m.Id,
                DeviceId = m.DeviceId,
                DeviceCode = m.AttendanceDevice != null ? m.AttendanceDevice.Code : string.Empty,
                DeviceName = m.AttendanceDevice != null ? m.AttendanceDevice.Name : string.Empty,
                DeviceUserId = m.DeviceUserId,
                EmployeeId = m.EmployeeId,
                EmployeeCode = m.Employee != null ? m.Employee.EmployeeCode : string.Empty,
                EmployeeFullName = m.Employee != null ? m.Employee.FullName : string.Empty,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<DeviceMappingResponseDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<DeviceMappingResponseDto?> GetByIdAsync(int id)
    {
        return await _context.DeviceEmployeeMappings
            .AsNoTracking()
            .Include(m => m.AttendanceDevice)
            .Include(m => m.Employee)
            .Where(m => m.Id == id)
            .Select(m => new DeviceMappingResponseDto
            {
                Id = m.Id,
                DeviceId = m.DeviceId,
                DeviceCode = m.AttendanceDevice != null ? m.AttendanceDevice.Code : string.Empty,
                DeviceName = m.AttendanceDevice != null ? m.AttendanceDevice.Name : string.Empty,
                DeviceUserId = m.DeviceUserId,
                EmployeeId = m.EmployeeId,
                EmployeeCode = m.Employee != null ? m.Employee.EmployeeCode : string.Empty,
                EmployeeFullName = m.Employee != null ? m.Employee.FullName : string.Empty,
                CreatedAt = m.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DeviceMappingResponseDto> CreateAsync(CreateDeviceMappingDto dto)
    {
        var normalizedDeviceUserId = dto.DeviceUserId.Trim();

        // 1. Kiểm tra DeviceId tồn tại
        var device = await _context.AttendanceDevices.FindAsync(dto.DeviceId);
        if (device == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy thiết bị có ID {dto.DeviceId}.");
        }

        // 2. Kiểm tra EmployeeId tồn tại
        var employee = await _context.Employees.FindAsync(dto.EmployeeId);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhân viên có ID {dto.EmployeeId}.");
        }

        // 3. Kiểm tra ràng buộc duy nhất uq_device_user (DeviceId, DeviceUserId)
        var isMappingExisted = await _context.DeviceEmployeeMappings
            .AnyAsync(m => m.DeviceId == dto.DeviceId && m.DeviceUserId == normalizedDeviceUserId);

        if (isMappingExisted)
        {
            throw new InvalidOperationException($"Mã người dùng '{normalizedDeviceUserId}' đã được gán trên thiết bị '{device.Name}'.");
        }

        var mapping = new DeviceEmployeeMapping
        {
            DeviceId = dto.DeviceId,
            DeviceUserId = normalizedDeviceUserId,
            EmployeeId = dto.EmployeeId,
            CreatedAt = DateTime.UtcNow
        };

        _context.DeviceEmployeeMappings.Add(mapping);
        await _context.SaveChangesAsync();

        return new DeviceMappingResponseDto
        {
            Id = mapping.Id,
            DeviceId = device.Id,
            DeviceCode = device.Code,
            DeviceName = device.Name,
            DeviceUserId = mapping.DeviceUserId,
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = employee.FullName,
            CreatedAt = mapping.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var mapping = await _context.DeviceEmployeeMappings.FindAsync(id);
        if (mapping == null)
        {
            return false;
        }

        _context.DeviceEmployeeMappings.Remove(mapping);
        await _context.SaveChangesAsync();
        return true;
    }
}