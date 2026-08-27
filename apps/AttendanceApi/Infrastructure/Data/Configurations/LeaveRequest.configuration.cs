using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("leave_requests", t =>
        {
            t.HasCheckConstraint("chk_leave_dates", "to_date >= from_date");
        });

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
               .HasColumnName("id")
               .UseIdentityByDefaultColumn();

        builder.Property(l => l.EmployeeId)
               .HasColumnName("employee_id")
               .IsRequired();

        // Enum -> String UPPERCASE
        builder.Property(l => l.LeaveType)
               .HasColumnName("leave_type")
               .HasMaxLength(30)
               .HasConversion(
                   v => v.ToString().ToUpper(),
                   v => Enum.Parse<LeaveType>(v, true)
               )
               .IsRequired();

        builder.Property(l => l.FromDate)
               .HasColumnName("from_date")
               .IsRequired();

        builder.Property(l => l.ToDate)
               .HasColumnName("to_date")
               .IsRequired();

        builder.Property(l => l.TotalDays)
               .HasColumnName("total_days")
               .HasPrecision(3, 1)
               .HasDefaultValue(1.0m);

        builder.Property(l => l.Reason)
               .HasColumnName("reason")
               .HasColumnType("text")
               .IsRequired();

        builder.Property(l => l.Status)
               .HasColumnName("status")
               .HasMaxLength(30)
               .HasConversion(
                   v => v.ToString().ToUpper(),
                   v => Enum.Parse<RequestStatus>(v, true)
               )
               .HasDefaultValue(RequestStatus.Pending);

        builder.Property(l => l.ApproverId)
               .HasColumnName("approver_id");

        builder.Property(l => l.ApprovedAt)
               .HasColumnName("approved_at");

        builder.Property(l => l.RejectionReason)
               .HasColumnName("rejection_reason")
               .HasColumnType("text");

        builder.Property(l => l.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(l => l.UpdatedAt)
               .HasColumnName("updated_at")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(l => new { l.EmployeeId, l.FromDate, l.ToDate })
               .HasDatabaseName("idx_leave_requests_emp");

        builder.HasIndex(l => l.Status)
               .HasDatabaseName("idx_leave_requests_status");

        // Foreign Keys
        builder.HasOne(l => l.Employee)
               .WithMany(e => e.LeaveRequests)
               .HasForeignKey(l => l.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("fk_leave_requests_employee");

        builder.HasOne(l => l.Approver)
               .WithMany(e => e.ApprovedLeaveRequests)
               .HasForeignKey(l => l.ApproverId)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("fk_leave_requests_approver");
    }
}