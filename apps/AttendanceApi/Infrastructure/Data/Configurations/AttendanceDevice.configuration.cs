using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class AttendanceDeviceConfiguration : IEntityTypeConfiguration<AttendanceDevice>
{
    public void Configure(EntityTypeBuilder<AttendanceDevice> builder)
    {
        builder.ToTable("attendance_devices");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.Code)
               .HasColumnName("code")
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(d => d.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(d => d.IpAddress)
               .HasColumnName("IpAddress")
               .IsRequired()
               .HasMaxLength(45);

        builder.Property(d => d.Port)
               .HasColumnName("Port")
               .HasDefaultValue(4370);

        builder.Property(d => d.Model)
               .HasColumnName("Model")
               .HasMaxLength(100);

        builder.Property(d => d.SerialNumber)
               .HasColumnName("SerialNumber")
               .HasMaxLength(100);

        builder.Property(d => d.Location)
               .HasColumnName("Location")
               .HasMaxLength(255);

        builder.Property(d => d.Status)
               .HasColumnName("Status")
               .HasMaxLength(20)
               .HasConversion(
                   v => v.ToString().ToUpper(),
                   v => Enum.Parse<DeviceStatus>(v, true)
               )
               .HasDefaultValue(DeviceStatus.Online);

        builder.Property(d => d.LastSyncAt)
               .HasColumnName("LastSyncAt");

        builder.Property(d => d.CreatedAt)
               .HasColumnName("CreatedAt")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(d => d.UpdatedAt)
               .HasColumnName("UpdatedAt")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(d => d.Code).IsUnique();
        builder.HasIndex(d => d.SerialNumber).IsUnique();
    }
}