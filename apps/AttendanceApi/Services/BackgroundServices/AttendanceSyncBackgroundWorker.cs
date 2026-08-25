using AttendanceApi.Services;


namespace AttendanceApi.Services.BackgroundServices;

public class AttendanceSyncBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AttendanceSyncBackgroundWorker> _logger;
    private readonly TimeSpan _syncInterval;

    public AttendanceSyncBackgroundWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AttendanceSyncBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var intervalSeconds = configuration.GetValue<int>("DeviceSync:IntervalSeconds", 300);
        _syncInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AttendanceSyncBackgroundWorker đã khởi động. Chu kỳ: {Seconds}s", _syncInterval.TotalSeconds);

        using var timer = new PeriodicTimer(_syncInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Bắt đầu phiên quét đồng bộ tự động từ các máy chấm công...");

                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IDeviceSyncService>();

                var results = await syncService.SyncAllActiveDevicesAsync("AUTO_SCHEDULED");

                _logger.LogInformation("Đã hoàn tất phiên quét tự động cho {Count} thiết bị.", results.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong chu kỳ AttendanceSyncBackgroundWorker: {Message}", ex.Message);
            }
        }

        _logger.LogInformation("AttendanceSyncBackgroundWorker đang dừng lại.");
    }
}