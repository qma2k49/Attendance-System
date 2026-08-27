using AttendanceApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
{
    public void Configure(EntityTypeBuilder<WorkShift> builder)
    {
        builder.ToTable("work_shifts");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");

        builder.Property(w => w.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(w => w.StartTime)
            .HasColumnName("start_time")
            .IsRequired();

        builder.Property(w => w.EndTime)
            .HasColumnName("end_time")
            .IsRequired();

        builder.Property(w => w.BreakStartTime)
            .HasColumnName("break_start_time");

        builder.Property(w => w.BreakEndTime)
            .HasColumnName("break_end_time");

        builder.Property(w => w.GracePeriodMinutes)
            .HasColumnName("grace_period_minutes")
            .HasDefaultValue(0);

        builder.Property(w => w.WorkHours)
            .HasColumnName("work_hours")
            .HasPrecision(4, 2)
            .HasDefaultValue(8.00m);

        builder.Property(w => w.IsOvernight)
            .HasColumnName("is_overnight")
            .HasDefaultValue(false);

        builder.Property(w => w.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(w => w.Code)
            .IsUnique()
            .HasDatabaseName("uq_work_shifts_code");
    }
}
