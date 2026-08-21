using AttendanceApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApi.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.Code)
               .HasColumnName("code")
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(d => d.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(d => d.CreatedAt)
               .HasColumnName("CreateAt")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(d => d.UpdatedAt)
               .HasColumnName("UpdateAt")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(d => d.Code).IsUnique();
    }
}