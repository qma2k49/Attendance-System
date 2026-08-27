using AttendanceApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.ToTable("work_schedules");

        builder.HasKey(ws => ws.Id);
        builder.Property(ws => ws.Id).HasColumnName("id");

        builder.Property(ws => ws.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.Property(ws => ws.WorkShiftId)
            .HasColumnName("work_shift_id")
            .IsRequired();

        builder.Property(ws => ws.WorkDate)
            .HasColumnName("work_date")
            .IsRequired();

        builder.Property(ws => ws.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(ws => ws.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Foreign Keys
        builder.HasOne(ws => ws.Employee)
            .WithMany(e => e.WorkSchedules)
            .HasForeignKey(ws => ws.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ws => ws.WorkShift)
            .WithMany(s => s.WorkSchedules)
            .HasForeignKey(ws => ws.WorkShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique Constraint uq_employee_work_schedule
        builder.HasIndex(ws => new { ws.EmployeeId, ws.WorkDate })
            .IsUnique()
            .HasDatabaseName("uq_employee_work_schedule");

        // Index idx_work_schedules_lookup
        builder.HasIndex(ws => new { ws.EmployeeId, ws.WorkDate })
            .HasDatabaseName("idx_work_schedules_lookup");
    }
}
