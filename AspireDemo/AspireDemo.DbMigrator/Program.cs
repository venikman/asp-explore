using AspireDemo.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("appdb");

var host = builder.Build();

await RunMigrationsAsync(host.Services);

// Exit after migrations complete (Aspire will see this as "completed")
return;

async Task RunMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("DbMigrator");
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

    logger.LogInformation("DbMigrator starting...");

    // Apply migrations
    var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (pendingMigrations.Count > 0)
    {
        logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
            pendingMigrations.Count,
            string.Join(", ", pendingMigrations));

        await db.Database.MigrateAsync();

        logger.LogInformation("Migrations applied successfully");
    }
    else
    {
        logger.LogInformation("Database is up to date");
    }

    // Seed demo data only in Development
    if (env.IsDevelopment())
    {
        await SeedDemoDataAsync(db, logger);
    }

    logger.LogInformation("DbMigrator completed successfully");
}

async Task SeedDemoDataAsync(AppDbContext db, ILogger logger)
{
    if (await db.Products.AnyAsync())
    {
        logger.LogDebug("Demo data already exists, skipping");
        return;
    }

    var categories = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
    if (categories.Count == 0)
    {
        logger.LogWarning("No categories found");
        return;
    }

    logger.LogInformation("Seeding demo products...");

    var products = new List<Product>
    {
        new() { Name = "Wireless Headphones", Description = "Bluetooth over-ear headphones with noise cancellation", Price = 149.99m, CategoryId = categories.GetValueOrDefault("Electronics") },
        new() { Name = "USB-C Hub", Description = "7-in-1 USB-C hub with HDMI and SD card reader", Price = 49.99m, CategoryId = categories.GetValueOrDefault("Electronics") },
        new() { Name = "Mechanical Keyboard", Description = "RGB mechanical keyboard with Cherry MX switches", Price = 129.99m, CategoryId = categories.GetValueOrDefault("Electronics") },
        new() { Name = "Cotton T-Shirt", Description = "100% organic cotton t-shirt", Price = 24.99m, CategoryId = categories.GetValueOrDefault("Clothing") },
        new() { Name = "Denim Jeans", Description = "Classic fit denim jeans", Price = 59.99m, CategoryId = categories.GetValueOrDefault("Clothing") },
        new() { Name = "Clean Code", Description = "A Handbook of Agile Software Craftsmanship", Price = 39.99m, CategoryId = categories.GetValueOrDefault("Books") },
        new() { Name = "Domain-Driven Design", Description = "Tackling Complexity in the Heart of Software", Price = 54.99m, CategoryId = categories.GetValueOrDefault("Books") },
        new() { Name = "LED Desk Lamp", Description = "Adjustable LED desk lamp with USB charging", Price = 34.99m, CategoryId = categories.GetValueOrDefault("Home & Garden") },
        new() { Name = "Yoga Mat", Description = "Non-slip exercise yoga mat", Price = 24.99m, CategoryId = categories.GetValueOrDefault("Sports") },
        new() { Name = "Resistance Bands", Description = "Set of 5 resistance bands", Price = 19.99m, CategoryId = categories.GetValueOrDefault("Sports") }
    };

    db.Products.AddRange(products);
    await db.SaveChangesAsync();

    logger.LogInformation("Seeded {Count} demo products", products.Count);
}
