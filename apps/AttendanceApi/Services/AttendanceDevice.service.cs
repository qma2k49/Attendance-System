using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using AttendanceApi.DTOs.Common;
using AttendanceApi.DTOs.Devices;
using AttendanceApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AttendanceApi.Services;

public class AttendanceDeviceService : IAttendanceDeviceService
{
    private readonly AttendanceDbContext _context;
    private readonly IMemoryCache? _cache;
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(10);

    private static readonly HashSet<string> DeviceCacheKeys = new();
    private static readonly object LockObj = new();

    public AttendanceDeviceService(AttendanceDbContext context, IMemoryCache? cache = null)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PagedResultDto<DeviceResponseDto>> GetPagedAsync(DeviceFilterDto filter)
    {
        var cacheKey = $"devices:paged:{filter.PageNumber}_{filter.PageSize}_{filter.Status?.ToString() ?? "all"}_{filter.Keyword?.Trim().ToLowerInvariant() ?? "none"}";

        if (_cache != null && _cache.TryGetValue(cacheKey, out PagedResultDto<DeviceResponseDto>? cachedPaged) && cachedPaged != null)
        {
            return cachedPaged;
        }

        var query = _context.AttendanceDevices
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim().ToLower();
            query = query.Where(d => d.Code.ToLower().Contains(keyword) || 
                                     d.Name.ToLower().Contains(keyword) ||
                                     d.IpAddress.ToLower().Contains(keyword));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(d => d.Status == filter.Status.Value);
        }

        var totalItems = await query.CountAsync();
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var items = await query
            .OrderBy(d => d.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DeviceResponseDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                IpAddress = d.IpAddress,
                Port = d.Port,
                Model = d.Model,
                SerialNumber = d.SerialNumber,
                Location = d.Location,
                Status = d.Status.ToString().ToUpper(),
                LastSyncAt = d.LastSyncAt,
                MappedEmployeeCount = d.DeviceEmployeeMappings.Count(),
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();

        var result = new PagedResultDto<DeviceResponseDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

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

    public async Task<DeviceResponseDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"devices:id:{id}";

        if (_cache != null && _cache.TryGetValue(cacheKey, out DeviceResponseDto? cachedDevice) && cachedDevice != null)
        {
            return cachedDevice;
        }

        var result = await _context.AttendanceDevices
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DeviceResponseDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                IpAddress = d.IpAddress,
                Port = d.Port,
                Model = d.Model,
                SerialNumber = d.SerialNumber,
                Location = d.Location,
                Status = d.Status.ToString().ToUpper(),
                LastSyncAt = d.LastSyncAt,
                MappedEmployeeCount = d.DeviceEmployeeMappings.Count(),
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

    public async Task<DeviceResponseDto> CreateAsync(CreateDeviceDto dto)
    {
        var normalizedCode = dto.Code.Trim().ToUpper();
        var normalizedSerial = dto.SerialNumber?.Trim();

        if (await _context.AttendanceDevices.AnyAsync(d => d.Code == normalizedCode))
        {
            throw new InvalidOperationException($"Mã thiết bị '{normalizedCode}' đã tồn tại trong hệ thống.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedSerial) && 
            await _context.AttendanceDevices.AnyAsync(d => d.SerialNumber == normalizedSerial))
        {
            throw new InvalidOperationException($"Số Serial '{normalizedSerial}' đã tồn tại trên một thiết bị khác.");
        }

        var device = new AttendanceDevice
        {
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            IpAddress = dto.IpAddress.Trim(),
            Port = dto.Port,
            Model = dto.Model?.Trim(),
            SerialNumber = normalizedSerial,
            Location = dto.Location?.Trim(),
            Status = DeviceStatus.Online,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AttendanceDevices.Add(device);
        await _context.SaveChangesAsync();

        InvalidateCache();

        return new DeviceResponseDto
        {
            Id = device.Id,
            Code = device.Code,
            Name = device.Name,
            IpAddress = device.IpAddress,
            Port = device.Port,
            Model = device.Model,
            SerialNumber = device.SerialNumber,
            Location = device.Location,
            Status = device.Status.ToString().ToUpper(),
            LastSyncAt = device.LastSyncAt,
            MappedEmployeeCount = 0,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt
        };
    }

    public async Task<DeviceResponseDto?> UpdateAsync(int id, UpdateDeviceDto dto)
    {
        var device = await _context.AttendanceDevices
            .Include(d => d.DeviceEmployeeMappings)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
        {
            return null;
        }

        var normalizedSerial = dto.SerialNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSerial) && 
            await _context.AttendanceDevices.AnyAsync(d => d.SerialNumber == normalizedSerial && d.Id != id))
        {
            throw new InvalidOperationException($"Số Serial '{normalizedSerial}' đã tồn tại trên một thiết bị khác.");
        }

        device.Name = dto.Name.Trim();
        device.IpAddress = dto.IpAddress.Trim();
        device.Port = dto.Port;
        device.Model = dto.Model?.Trim();
        device.SerialNumber = normalizedSerial;
        device.Location = dto.Location?.Trim();
        device.Status = dto.Status;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        InvalidateCache();

        return new DeviceResponseDto
        {
            Id = device.Id,
            Code = device.Code,
            Name = device.Name,
            IpAddress = device.IpAddress,
            Port = device.Port,
            Model = device.Model,
            SerialNumber = device.SerialNumber,
            Location = device.Location,
            Status = device.Status.ToString().ToUpper(),
            LastSyncAt = device.LastSyncAt,
            MappedEmployeeCount = device.DeviceEmployeeMappings.Count,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var device = await _context.AttendanceDevices.FindAsync(id);
        if (device == null)
        {
            return false;
        }

        _context.AttendanceDevices.Remove(device);
        await _context.SaveChangesAsync();

        InvalidateCache();
        return true;
    }

    private static void TrackCacheKey(string key)
    {
        lock (LockObj)
        {
            DeviceCacheKeys.Add(key);
        }
    }

    private void InvalidateCache()
    {
        if (_cache == null) return;

        lock (LockObj)
        {
            foreach (var key in DeviceCacheKeys)
            {
                _cache.Remove(key);
            }
            DeviceCacheKeys.Clear();
        }
    }
}