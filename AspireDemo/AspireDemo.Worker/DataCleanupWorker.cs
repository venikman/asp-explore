using AspireDemo.Data.Analytics;
using Microsoft.EntityFrameworkCore;

namespace AspireDemo.Worker;

/// <summary>
/// Example worker that performs periodic data cleanup.
/// This worker is configured to start manually via the Aspire dashboard.
///
/// Use cases for manual-start workers:
/// - Data cleanup/archival jobs
/// - Report generation
/// - One-time maintenance tasks
/// - On-demand batch processing
/// </summary>
public class DataCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataCleanupWorker> _logger;

    public DataCleanupWorker(
        IServiceProvider serviceProvider,
        ILogger<DataCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DataCleanupWorker started at {Time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup cycle");
            }

            // Run every 30 seconds while worker is active
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("DataCleanupWorker stopped at {Time}", DateTimeOffset.Now);
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();

        // Example: Delete page views older than 30 days
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        var oldPageViews = await db.PageViews
            .Where(p => p.Timestamp < cutoffDate)
            .CountAsync(cancellationToken);

        if (oldPageViews > 0)
        {
            _logger.LogInformation("Found {Count} old page views to clean up", oldPageViews);

            // Delete in batches to avoid long transactions
            var deleted = await db.PageViews
                .Where(p => p.Timestamp < cutoffDate)
                .Take(1000)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} old page views", deleted);
        }
        else
        {
            _logger.LogDebug("No old page views to clean up");
        }

        // Example: Delete old user events
        var oldEvents = await db.UserEvents
            .Where(e => e.Timestamp < cutoffDate)
            .CountAsync(cancellationToken);

        if (oldEvents > 0)
        {
            _logger.LogInformation("Found {Count} old user events to clean up", oldEvents);

            var deleted = await db.UserEvents
                .Where(e => e.Timestamp < cutoffDate)
                .Take(1000)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} old user events", deleted);
        }
        else
        {
            _logger.LogDebug("No old user events to clean up");
        }
    }
}
