using AspireDemo.Data.Analytics;
using AspireDemo.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AnalyticsDbContext>("analyticsdb");

builder.Services.AddHostedService<DataCleanupWorker>();

var host = builder.Build();
host.Run();
