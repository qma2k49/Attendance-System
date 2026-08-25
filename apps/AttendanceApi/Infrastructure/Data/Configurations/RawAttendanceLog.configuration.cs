using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class RawAttendanceLogConfiguration : IEntityTypeConfiguration<RawAttendanceLog>
{
    public void Configure(EntityTypeBuilder<RawAttendanceLog> builder)
    {
        builder.ToTable("raw_attendance_logs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
               .HasColumnName("id")
               .UseIdentityByDefaultColumn();

        builder.Property(r => r.DeviceId)
               .HasColumnName("device_id")
               .IsRequired();

        builder.Property(r => r.DeviceUserId)
               .HasColumnName("device_user_id")
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(r => r.CheckTime)
               .HasColumnName("check_time")
               .IsRequired();

        // Map Enum sang String UPPERCASE
        builder.Property(r => r.VerifyMode)
               .HasColumnName("verify_mode")
               .HasMaxLength(30)
               .HasConversion(
                   v => v.ToString().ToUpper(),
                   v => Enum.Parse<VerifyModeEnum>(v, true)
               )
               .HasDefaultValue(VerifyModeEnum.Fingerprint);

        builder.Property(r => r.ProcessedStatus)
               .HasColumnName("processed_status")
               .HasMaxLength(30)
               .HasConversion(
                   p => p.ToString().ToUpper(),
                   p => Enum.Parse<ProcessedStatus>(p, true)
               )
               .HasDefaultValue(ProcessedStatus.Pending);

        builder.Property(r => r.RawPayload)
               .HasColumnName("raw_payload")
               .HasColumnType("text");

        builder.Property(r => r.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Ràng buộc duy nhất chống trùng log: (DeviceId, DeviceUserId, CheckTime)
        builder.HasIndex(r => new { r.DeviceId, r.DeviceUserId, r.CheckTime })
               .IsUnique()
               .HasDatabaseName("uq_raw_logs_dedup");

        // Index cho tiến trình quét tính công

        builder.HasIndex(r => r.ProcessedStatus)
               .HasDatabaseName("idx_raw_logs_status");

        // Foreign Key: DeviceId -> AttendanceDevice
        builder.HasOne(r => r.AttendanceDevice)
               .WithMany(d => d.RawAttendanceLogs)
               .HasForeignKey(r => r.DeviceId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("fk_raw_logs_device");
    }
}