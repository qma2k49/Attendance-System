using AttendanceApi.Domain.Entities;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AttendanceDbContext>(
    options => 
        options.UseNpgsql(connectionString)
    );

builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAttendanceDeviceService, AttendanceDeviceService>();
builder.Services.AddScoped<IDeviceEmployeeMappingService, DeviceEmployeeMappingService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();


app.UseAuthorization();

app.MapControllers();

app.Run();
