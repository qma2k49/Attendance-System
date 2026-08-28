using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class MonthlyTimesheetSummaryConfiguration : IEntityTypeConfiguration<MonthlyTimesheetSummary>
{
    public void Configure(EntityTypeBuilder<MonthlyTimesheetSummary> builder)
    {
        builder.ToTable("monthly_timesheet_summaries", t =>
        {
            t.HasCheckConstraint("chk_timesheet_month", "month >= 1 AND month <= 12");
            t.HasCheckConstraint("chk_timesheet_year", "year >= 2000");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
               .HasColumnName("id")
               .UseIdentityByDefaultColumn();

        builder.Property(m => m.EmployeeId)
               .HasColumnName("employee_id")
               .IsRequired();

        builder.Property(m => m.Year)
               .HasColumnName("year")
               .IsRequired();

        builder.Property(m => m.Month)
               .HasColumnName("month")
               .IsRequired();

        builder.Property(m => m.StandardWorkingDays)
               .HasColumnName("standard_working_days")
               .HasPrecision(4, 1)
               .HasDefaultValue(0.0m);

        builder.Property(m => m.ActualWorkingDays)
               .HasColumnName("actual_working_days")
               .HasPrecision(4, 1)
               .HasDefaultValue(0.0m);

        builder.Property(m => m.ActualWorkingHours)
               .HasColumnName("actual_working_hours")
               .HasPrecision(6, 2)
               .HasDefaultValue(0.00m);

        builder.Property(m => m.PaidLeaveDays)
               .HasColumnName("paid_leave_days")
               .HasPrecision(4, 1)
               .HasDefaultValue(0.0m);

        builder.Property(m => m.UnpaidLeaveDays)
               .HasColumnName("unpaid_leave_days")
               .HasPrecision(4, 1)
               .HasDefaultValue(0.0m);

        builder.Property(m => m.AbsentDays)
               .HasColumnName("absent_days")
               .HasPrecision(4, 1)
               .HasDefaultValue(0.0m);

        builder.Property(m => m.LateMinutes)
               .HasColumnName("late_minutes")
               .HasDefaultValue(0);

        builder.Property(m => m.EarlyMinutes)
               .HasColumnName("early_minutes")
               .HasDefaultValue(0);

        builder.Property(m => m.LateOccurrences)
               .HasColumnName("late_occurrences")
               .HasDefaultValue(0);

        builder.Property(m => m.EarlyOccurrences)
               .HasColumnName("early_occurrences")
               .HasDefaultValue(0);

        builder.Property(m => m.OvertimeHours)
               .HasColumnName("overtime_hours")
               .HasPrecision(6, 2)
               .HasDefaultValue(0.00m);

        builder.Property(m => m.TotalPayableDays)
               .HasColumnName("total_payable_days")
               .HasPrecision(4, 1)
               .HasDefaultValue(0.0m);

        // Enum -> UPPERCASE String
        builder.Property(m => m.Status)
               .HasColumnName("status")
               .HasMaxLength(30)
               .HasConversion(
                   v => v.ToString().ToUpper(),
                   v => Enum.Parse<TimesheetStatus>(v, true)
               )
               .HasDefaultValueSql("'DRAFT'");

        builder.Property(m => m.FinalizedBy)
               .HasColumnName("finalized_by");

        builder.Property(m => m.FinalizedAt)
               .HasColumnName("finalized_at");

        builder.Property(m => m.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(m => m.UpdatedAt)
               .HasColumnName("updated_at")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Unique Constraint
        builder.HasIndex(m => new { m.EmployeeId, m.Year, m.Month })
               .IsUnique()
               .HasDatabaseName("uq_monthly_timesheet_emp_period");

        // Indexes
        builder.HasIndex(m => new { m.Year, m.Month })
               .HasDatabaseName("idx_monthly_timesheets_period");

        builder.HasIndex(m => m.Status)
               .HasDatabaseName("idx_monthly_timesheets_status");

        // Foreign Keys
        builder.HasOne(m => m.Employee)
               .WithMany(e => e.MonthlyTimesheetSummaries)
               .HasForeignKey(m => m.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("fk_monthly_timesheets_employee");

        builder.HasOne(m => m.Finalizer)
               .WithMany(e => e.FinalizedMonthlyTimesheets)
               .HasForeignKey(m => m.FinalizedBy)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("fk_monthly_timesheets_finalizer");
    }
}