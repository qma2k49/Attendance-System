using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class DeviceSyncLogConfiguration : IEntityTypeConfiguration<DeviceSyncLog>
{
    public void Configure(EntityTypeBuilder<DeviceSyncLog> builder)
    {
        builder.ToTable("device_sync_logs");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
               .HasColumnName("id")
               .UseIdentityByDefaultColumn();

        builder.Property(s => s.DeviceId)
               .HasColumnName("device_id")
               .IsRequired();

        builder.Property(s => s.SyncType)
               .HasColumnName("sync_type")
               .HasMaxLength(50)
               .HasDefaultValue("AUTO_SCHEDULED")
               .IsRequired();

        builder.Property(s => s.RecordsPulled)
               .HasColumnName("records_pulled")
               .HasDefaultValue(0);

        builder.Property(s => s.RecordsInserted)
               .HasColumnName("records_inserted")
               .HasDefaultValue(0);

        builder.Property(s => s.Status)
               .HasColumnName("status")
               .HasMaxLength(30)
               .HasConversion(
                   s => s.ToString().ToUpper(),
                   s => Enum.Parse<SyncStatus>(s, true)
               )
               .HasDefaultValue(SyncStatus.Success);

        builder.Property(s => s.ErrorMessage)
               .HasColumnName("error_message")
               .HasColumnType("text");

        builder.Property(s => s.SyncedAt)
               .HasColumnName("synced_at")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Index: idx_sync_logs_device_id
        builder.HasIndex(s => s.DeviceId)
               .HasDatabaseName("idx_sync_logs_device_id");

        // Foreign Key: DeviceId -> AttendanceDevice
        builder.HasOne(s => s.AttendanceDevice)
               .WithMany(d => d.DeviceSyncLogs)
               .HasForeignKey(s => s.DeviceId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("fk_sync_logs_device");
    }
}
