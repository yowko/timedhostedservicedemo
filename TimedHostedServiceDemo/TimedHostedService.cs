using System.Timers;
using Cronos;

namespace TimedHostedServiceDemo;
public class TimedHostedService : IHostedService, IDisposable
{
    private readonly ILogger<TimedHostedService> _logger;
    private System.Timers.Timer _timer = null;
    private readonly CronExpression _expression;
    private readonly TimeZoneInfo _timeZoneInfo;
    private readonly UpdateCacheService _updateCacheService;

    public TimedHostedService(ILogger<TimedHostedService> logger,UpdateCacheService updateCacheService)
    {
        _logger = logger;
        _expression = CronExpression.Parse("00 00 * * *");
        _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        _updateCacheService = updateCacheService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _updateCacheService.GetAsync(cancellationToken);
        await ScheduleJob(cancellationToken);
    }

    private async Task ScheduleJob(CancellationToken cancellationToken)
    {
        var next = _expression.GetNextOccurrence(DateTimeOffset.Now, _timeZoneInfo);
        if (next.HasValue)
        {
            var delay = next.Value - DateTimeOffset.Now;
            if (delay.TotalMilliseconds <= 0)
            {
                await ScheduleJob(cancellationToken);
            }

            _timer = new System.Timers.Timer(delay.TotalMilliseconds);

            async void OnTimerOnElapsed(object sender, ElapsedEventArgs args)
            {
                _timer.Dispose();
                _timer = null;

                if (!cancellationToken.IsCancellationRequested)
                {
                    await DoWork(cancellationToken);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await ScheduleJob(cancellationToken);
                }
            }

            _timer.Elapsed += OnTimerOnElapsed;
            _timer.Start();
        }

        await Task.CompletedTask;
    }

    public async Task DoWork(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{DateTime.Now}:test cron job");

        await _updateCacheService.GetAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping");

        _timer?.Stop();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}