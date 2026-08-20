using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Infrastructure.Data;

public class AttendanceDbContext : DbContext
{
    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : base(options)
    {
    }
}