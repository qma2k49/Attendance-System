using AttendanceApi.Domain.Entities;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using AttendanceApi.Services.BackgroundServices;


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
builder.Services.AddScoped<IIngestionService, IngestionService>();
builder.Services.AddScoped<IDeviceSyncService, DeviceSyncService>();
builder.Services.AddHostedService<AttendanceSyncBackgroundWorker>();


builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();


app.UseAuthorization();

app.MapControllers();

app.Run();
