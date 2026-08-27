using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class AttendanceAdjustmentConfiguration : IEntityTypeConfiguration<AttendanceAdjustment>
{
    public void Configure(EntityTypeBuilder<AttendanceAdjustment> builder)
    {
        builder.ToTable("attendance_adjustments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
               .HasColumnName("id")
               .UseIdentityByDefaultColumn();

        builder.Property(a => a.EmployeeId)
               .HasColumnName("employee_id")
               .IsRequired();

        builder.Property(a => a.WorkDate)
               .HasColumnName("work_date")
               .IsRequired();

        // Enum -> String UPPERCASE
        builder.Property(a => a.AdjustmentType)
               .HasColumnName("adjustment_type")
               .HasMaxLength(30)
               .HasConversion(
                   v => v.ToString().ToUpper(),
                   v => Enum.Parse<AdjustmentType>(v, true)
               )
               .IsRequired();

        builder.Property(a => a.AdjustedCheckIn)
               .HasColumnName("adjusted_check_in");

        builder.Property(a => a.AdjustedCheckOut)
               .HasColumnName("adjusted_check_out");

        builder.Property(a => a.Reason)
               .HasColumnName("reason")
               .HasColumnType("text")
               .IsRequired();

        builder.Property(a => a.Status)
               .HasColumnName("status")
               .HasMaxLength(30)
               .HasConversion(
                   v => v.ToString().ToUpper(),
                   v => Enum.Parse<RequestStatus>(v, true)
               )
               .HasDefaultValue(RequestStatus.Pending);

        builder.Property(a => a.ApproverId)
               .HasColumnName("approver_id");

        builder.Property(a => a.ApprovedAt)
               .HasColumnName("approved_at");

        builder.Property(a => a.RejectionReason)
               .HasColumnName("rejection_reason")
               .HasColumnType("text");

        builder.Property(a => a.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(a => a.UpdatedAt)
               .HasColumnName("updated_at")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(a => new { a.EmployeeId, a.WorkDate })
               .HasDatabaseName("idx_adjustments_emp_date");

        builder.HasIndex(a => a.Status)
               .HasDatabaseName("idx_adjustments_status");

        // Foreign Keys
        builder.HasOne(a => a.Employee)
               .WithMany(e => e.AttendanceAdjustments)
               .HasForeignKey(a => a.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("fk_adjustments_employee");

        builder.HasOne(a => a.Approver)
               .WithMany(e => e.ApprovedAttendanceAdjustments)
               .HasForeignKey(a => a.ApproverId)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("fk_adjustments_approver");
    }
}