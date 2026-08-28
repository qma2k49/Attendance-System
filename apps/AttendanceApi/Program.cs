using AttendanceApi.Domain.Entities;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using AttendanceApi.Services.BackgroundServices;
using AttendanceApi.Hubs;

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
builder.Services.AddScoped<IRawAttendanceLogService, RawAttendanceLogService>();
builder.Services.AddScoped<IAttendanceProcessingEngine, AttendanceProcessingEngine>();
builder.Services.AddScoped<IDailyAttendanceService, DailyAttendanceService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IAttendanceAdjustmentService, AttendanceAdjustmentService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<ITimesheetAggregationService, TimesheetAggregationService>();
builder.Services.AddScoped<IMonthlyTimesheetService, MonthlyTimesheetService>();


builder.Services.AddSignalR();
builder.Services.AddControllers();

builder.Services.AddHostedService<DailyAttendanceProcessingWorker>();

var app = builder.Build();

app.MapHub<AttendanceHub>("/hubs/attendance");
app.UseHttpsRedirection();



app.UseAuthorization();

app.MapControllers();

app.Run();
