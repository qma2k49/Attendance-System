using AttendanceApi.DTOs.AttendanceProcessing;
using AttendanceApi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AttendanceApi.Services.BackgroundServices;

public class DailyAttendanceProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyAttendanceProcessingWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1); // Chạy định kỳ mỗi 1 giờ

    public DailyAttendanceProcessingWorker(
        IServiceProvider serviceProvider, 
        ILogger<DailyAttendanceProcessingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyAttendanceProcessingWorker đã khởi động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                _logger.LogInformation("Bắt đầu chu kỳ tính công tự động cho ngày {WorkDate}", today);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var engine = scope.ServiceProvider.GetRequiredService<IAttendanceProcessingEngine>();
                    
                    var result = await engine.ProcessDailyAttendanceAsync(
                        new ProcessAttendanceRequestDto { WorkDate = today }, 
                        stoppingToken);

                    _logger.LogInformation("Kết quả tính công tự động: {Message} | Thành công: {Success}/{Total}",
                        result.Message, result.SuccessCount, result.TotalEmployeesProcessed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Dừng worker an toàn
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thực thi tính công tự động tại DailyAttendanceProcessingWorker.");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("DailyAttendanceProcessingWorker đã dừng.");
    }
}