using AttendanceApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AttendanceApi.Infrastructure.Data;


public class AttendanceDbContext : DbContext
{
    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceDevice> AttendanceDevices => Set<AttendanceDevice>();
    public DbSet<DeviceEmployeeMapping> DeviceEmployeeMappings => Set<DeviceEmployeeMapping>();

    public DbSet<RawAttendanceLog> RawAttendanceLogs => Set<RawAttendanceLog>();
    public DbSet<DeviceSyncLog> DeviceSyncLogs => Set<DeviceSyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Tự động quét và áp dụng tất cả IEntityTypeConfiguration trong Assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}