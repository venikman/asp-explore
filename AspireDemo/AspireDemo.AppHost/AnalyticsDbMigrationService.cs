using Aspire.Hosting;
using AspireDemo.Data.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspireDemo.AppHost;

/// <summary>
/// Hosted service that runs AnalyticsDb migrations directly from AppHost.
/// This demonstrates Pattern 2: Running migrations from the orchestrator.
///
/// Trade-offs vs separate migrator project:
/// - Pro: Fewer projects, simpler solution structure
/// - Con: No OpenTelemetry tracing for migration, harder to debug
/// - Con: Couples orchestration with database schema
/// </summary>
public class AnalyticsDbMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ResourceNotificationService _resourceNotification;
    private readonly ILogger<AnalyticsDbMigrationService> _logger;

    public AnalyticsDbMigrationService(
        IServiceProvider serviceProvider,
        ResourceNotificationService resourceNotification,
        ILogger<AnalyticsDbMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _resourceNotification = resourceNotification;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Waiting for analyticsdb to be ready...");

            // Wait for the database resource to be running and healthy
            await _resourceNotification.WaitForResourceAsync(
                "analyticsdb",
                KnownResourceStates.Running,
                stoppingToken);

            _logger.LogInformation("analyticsdb is ready, running migrations from AppHost...");

            // Get the connection string from Aspire's configuration
            // The connection string is available via IConfiguration after resource starts
            var connectionString = await GetConnectionStringAsync(stoppingToken);

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Failed to get connection string for analyticsdb");
                return;
            }

            // Run migrations
            await RunMigrationsAsync(connectionString, stoppingToken);

            _logger.LogInformation("AnalyticsDb migrations completed from AppHost");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Migration service cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running AnalyticsDb migrations");
        }
    }

    private async Task<string?> GetConnectionStringAsync(CancellationToken cancellationToken)
    {
        // Poll for connection string (it becomes available after resource starts)
        var config = _serviceProvider.GetRequiredService<IConfiguration>();

        for (var i = 0; i < 30; i++)
        {
            var connectionString = config.GetConnectionString("analyticsdb");
            if (!string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }

            await Task.Delay(1000, cancellationToken);
        }

        return null;
    }

    private async Task RunMigrationsAsync(string connectionString, CancellationToken cancellationToken)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AnalyticsDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(AnalyticsDbContext).Assembly.FullName);
            options.MigrationsHistoryTable("__EFMigrationsHistory_Analytics");
        });

        await using var dbContext = new AnalyticsDbContext(optionsBuilder.Options);

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count > 0)
        {
            _logger.LogInformation(
                "[AppHost] Applying {Count} pending migrations to AnalyticsDb: {Migrations}",
                pendingMigrations.Count,
                string.Join(", ", pendingMigrations));

            await dbContext.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("[AppHost] AnalyticsDb migrations applied successfully");
        }
        else
        {
            _logger.LogInformation("[AppHost] AnalyticsDb is up to date");
        }
    }
}
