using AspireDemo.AppHost;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================================
// PATTERN 1: Separate Migrator Project (Production-Ready)
// ============================================================================
// - Migrations run in dedicated worker project
// - Full OpenTelemetry observability
// - Can restart migrations independently via dashboard
// - Recommended for production deployments
// ============================================================================

var postgres1 = builder.AddPostgres("postgres")
    .WithImageRegistry("ghcr.io")
    .WithImage("venikman/postgres")
    .WithImageTag("16-alpine")
    .WithPgAdmin();

var appDb = postgres1.AddDatabase("appdb");

// Migrator runs first, exits on completion
var migrator = builder.AddProject<Projects.AspireDemo_DbMigrator>("migrator")
    .WithReference(appDb)
    .WaitFor(appDb);

// ============================================================================
// PATTERN 2: Migrations from AppHost (Simpler, Demo-Oriented)
// ============================================================================
// - Migrations run via HostedService in AppHost
// - Fewer projects, simpler structure
// - Limited observability (no distributed tracing)
// - Suitable for development/prototyping
// ============================================================================

var postgres2 = builder.AddPostgres("postgres-analytics")
    .WithImageRegistry("ghcr.io")
    .WithImage("venikman/postgres")
    .WithImageTag("16-alpine");

var analyticsDb = postgres2.AddDatabase("analyticsdb");

// Register the migration service that runs from AppHost
builder.Services.AddHostedService<AnalyticsDbMigrationService>();

// ============================================================================
// API Project - Connects to Both Databases
// ============================================================================

var api = builder.AddProject<Projects.AspireDemo_Api>("api")
    .WithReference(appDb)
    .WithReference(analyticsDb)
    .WaitForCompletion(migrator);  // Waits for Pattern 1 migrator
    // Note: Pattern 2 migrations run in parallel via HostedService

// ============================================================================
// Manual-Start Worker (start via dashboard when needed)
// ============================================================================
// - Does NOT start automatically with the application
// - Start manually via Aspire dashboard "Start" button
// - Use cases: cleanup jobs, reports, maintenance tasks
// ============================================================================

builder.AddProject<Projects.AspireDemo_Worker>("worker")
    .WithReference(analyticsDb)
    .WaitFor(analyticsDb)
    .WithExplicitStart();  // <-- Won't auto-start, use dashboard to start

builder.Build().Run();
