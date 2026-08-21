using AttendanceApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class DeviceEmployeeMappingConfiguration : IEntityTypeConfiguration<DeviceEmployeeMapping>
{
    public void Configure(EntityTypeBuilder<DeviceEmployeeMapping> builder)
    {
        builder.ToTable("device_employee_mappings");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.DeviceId)
               .HasColumnName("DeviceId")
               .IsRequired();

        builder.Property(m => m.DeviceUserId)
               .HasColumnName("DeviceUserId")
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(m => m.EmployeeId)
               .HasColumnName("EmployeeId")
               .IsRequired();

        builder.Property(m => m.CreatedAt)
               .HasColumnName("CreatedAt")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Unique Composite Index: uq_device_user
        builder.HasIndex(m => new { m.DeviceId, m.DeviceUserId })
               .IsUnique()
               .HasDatabaseName("uq_device_user");

        // FK AttendanceDevice - ON DELETE CASCADE
        builder.HasOne(m => m.AttendanceDevice)
               .WithMany(d => d.DeviceEmployeeMappings)
               .HasForeignKey(m => m.DeviceId)
               .OnDelete(DeleteBehavior.Cascade);

        // FK Employee - ON DELETE CASCADE
        builder.HasOne(m => m.Employee)
               .WithMany(e => e.DeviceEmployeeMappings)
               .HasForeignKey(m => m.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}