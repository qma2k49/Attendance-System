using AttendanceApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class DailyAttendanceRecordConfiguration : IEntityTypeConfiguration<DailyAttendanceRecord>
{
    public void Configure(EntityTypeBuilder<DailyAttendanceRecord> builder)
    {
        builder.ToTable("daily_attendance_records");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.Property(d => d.WorkShiftId)
            .HasColumnName("work_shift_id");

        builder.Property(d => d.WorkDate)
            .HasColumnName("work_date")
            .IsRequired();

        builder.Property(d => d.CheckInTime)
            .HasColumnName("check_in_time");

        builder.Property(d => d.CheckOutTime)
            .HasColumnName("check_out_time");

        builder.Property(d => d.LateMinutes)
            .HasColumnName("late_minutes")
            .HasDefaultValue(0);

        builder.Property(d => d.EarlyMinutes)
            .HasColumnName("early_minutes")
            .HasDefaultValue(0);

        builder.Property(d => d.WorkHours)
            .HasColumnName("work_hours")
            .HasPrecision(4, 2)
            .HasDefaultValue(0.00m);

        builder.Property(d => d.OvertimeHours)
            .HasColumnName("overtime_hours")
            .HasPrecision(4, 2)
            .HasDefaultValue(0.00m);

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValueSql("'ABSENT'")
            .IsRequired();

        builder.Property(d => d.ProcessedAt)
            .HasColumnName("processed_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Foreign Keys
        builder.HasOne(d => d.Employee)
            .WithMany(e => e.DailyAttendanceRecords)
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.WorkShift)
            .WithMany(s => s.DailyAttendanceRecords)
            .HasForeignKey(d => d.WorkShiftId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique Constraint uq_daily_records_emp_date
        builder.HasIndex(d => new { d.EmployeeId, d.WorkDate })
            .IsUnique()
            .HasDatabaseName("uq_daily_records_emp_date");

        // Index idx_daily_records_date
        builder.HasIndex(d => d.WorkDate)
            .HasDatabaseName("idx_daily_records_date");

        // Index idx_daily_records_status
        builder.HasIndex(d => d.Status)
            .HasDatabaseName("idx_daily_records_status");
    }
}
