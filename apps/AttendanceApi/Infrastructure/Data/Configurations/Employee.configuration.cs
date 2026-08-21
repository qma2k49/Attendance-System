using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.EmployeeCode)
               .HasColumnName("EmployeeCode")
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.FullName)
               .HasColumnName("FullName")
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(e => e.DepartmentId)
               .HasColumnName("DepartmentId");

        builder.Property(e => e.Position)
               .HasColumnName("Position")
               .HasMaxLength(100);

        builder.Property(e => e.StartDate)
               .HasColumnName("StartDate")
               .IsRequired();

        builder.Property(e => e.EndDate)
               .HasColumnName("EndDate");

        builder.Property(e => e.Status)
               .HasColumnName("Status")
               .HasMaxLength(20)
               .HasConversion(
                   v => v.ToString().ToUpper(),
                   v => Enum.Parse<EmployeeStatus>(v, true)
               )
               .HasDefaultValue(EmployeeStatus.Active);

        builder.Property(e => e.CreatedAt)
               .HasColumnName("CreatedAt")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("UpdatedAt")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(e => e.EmployeeCode).IsUnique();

        // FK Department - ON DELETE SET NULL
        builder.HasOne(e => e.Department)
               .WithMany(d => d.Employees)
               .HasForeignKey(e => e.DepartmentId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}