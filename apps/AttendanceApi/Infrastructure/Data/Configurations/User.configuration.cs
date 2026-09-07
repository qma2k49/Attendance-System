using AttendanceApi.Domain.Entities;
using AttendanceApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(u => u.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(30)
            .HasConversion(v => v.ToString().ToUpper(), v => Enum.Parse<UserRole>(v, true))
            .HasDefaultValue(UserRole.Employee);

        builder.Property(u => u.EmployeeId).HasColumnName("employee_id");
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(u => u.Username).IsUnique().HasDatabaseName("uq_users_username");
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("uq_users_email");

        builder.HasOne(u => u.Employee)
            .WithMany()
            .HasForeignKey(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_users_employee");
    }
}