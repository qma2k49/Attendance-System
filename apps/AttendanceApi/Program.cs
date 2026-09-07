using AttendanceApi.Domain.Entities;
using AttendanceApi.Infrastructure.Data;
using AttendanceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AttendanceApi.Services.BackgroundServices;
using AttendanceApi.Hubs;
using AttendanceApi.Common;
using System.Text;
using AttendanceApi.Common.Constants;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình JwtOptions
var jwtSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtOptions>(jwtSection);
var jwtOptions = jwtSection.Get<JwtOptions>()!;

// Cấu hình JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

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
builder.Services.AddScoped<ITimesheetExportService, TimesheetExportService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();



builder.Services.AddSignalR();
builder.Services.AddControllers();

builder.Services.AddHostedService<DailyAttendanceProcessingWorker>();

var app = builder.Build();

app.MapHub<AttendanceHub>("/hubs/attendance");
app.UseHttpsRedirection();



app.UseAuthorization();

app.MapControllers();

app.Run();
