using AspireDemo.Api.Data;
using AspireDemo.Data.Analytics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, resilience)
builder.AddServiceDefaults();

// Add PostgreSQL DbContexts - connection strings injected by Aspire
// Pattern 1: appdb - migrations run by separate DbMigrator project
builder.AddNpgsqlDbContext<AppDbContext>("appdb");

// Pattern 2: analyticsdb - migrations run from AppHost
builder.AddNpgsqlDbContext<AnalyticsDbContext>("analyticsdb");

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Map Aspire health endpoints
app.MapDefaultEndpoints();

// Development: Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============================================================================
// API Endpoints
// ============================================================================

var products = app.MapGroup("/api/products")
    .WithTags("Products");

products.MapGet("/", async (AppDbContext db) =>
{
    var items = await db.Products.ToListAsync();
    return TypedResults.Ok(items);
})
.WithName("GetAllProducts")
.WithSummary("Get all products");

products.MapGet("/{id:int}", async Task<IResult> (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    return product is not null
        ? TypedResults.Ok(product)
        : TypedResults.NotFound();
})
.WithName("GetProductById")
.WithSummary("Get a product by ID");

products.MapPost("/", async (CreateProductRequest request, AppDbContext db) =>
{
    var product = new Product
    {
        Name = request.Name,
        Description = request.Description,
        Price = request.Price
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/api/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithSummary("Create a new product");

products.MapDelete("/{id:int}", async Task<IResult> (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null)
    {
        return TypedResults.NotFound();
    }

    db.Products.Remove(product);
    await db.SaveChangesAsync();

    return TypedResults.NoContent();
})
.WithName("DeleteProduct")
.WithSummary("Delete a product");

// ============================================================================
// Analytics Endpoints (Pattern 2: migrations from AppHost)
// ============================================================================

var analytics = app.MapGroup("/api/analytics")
    .WithTags("Analytics");

analytics.MapPost("/pageview", async (PageViewRequest request, AnalyticsDbContext db, HttpContext http) =>
{
    var pageView = new PageView
    {
        Path = request.Path,
        UserAgent = http.Request.Headers.UserAgent.ToString(),
        IpAddress = http.Connection.RemoteIpAddress?.ToString()
    };

    db.PageViews.Add(pageView);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/api/analytics/pageviews/{pageView.Id}", pageView);
})
.WithName("TrackPageView")
.WithSummary("Track a page view");

analytics.MapPost("/event", async (UserEventRequest request, AnalyticsDbContext db) =>
{
    var userEvent = new UserEvent
    {
        EventName = request.EventName,
        UserId = request.UserId,
        Properties = request.Properties
    };

    db.UserEvents.Add(userEvent);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/api/analytics/events/{userEvent.Id}", userEvent);
})
.WithName("TrackEvent")
.WithSummary("Track a user event");

analytics.MapGet("/pageviews", async (AnalyticsDbContext db) =>
{
    var pageViews = await db.PageViews
        .OrderByDescending(p => p.Timestamp)
        .Take(100)
        .ToListAsync();
    return TypedResults.Ok(pageViews);
})
.WithName("GetPageViews")
.WithSummary("Get recent page views");

analytics.MapGet("/events", async (AnalyticsDbContext db, string? eventName) =>
{
    var query = db.UserEvents.AsQueryable();

    if (!string.IsNullOrEmpty(eventName))
    {
        query = query.Where(e => e.EventName == eventName);
    }

    var events = await query
        .OrderByDescending(e => e.Timestamp)
        .Take(100)
        .ToListAsync();
    return TypedResults.Ok(events);
})
.WithName("GetEvents")
.WithSummary("Get recent user events");

app.Run();

// Request DTOs
public record CreateProductRequest(string Name, string? Description, decimal Price);
public record PageViewRequest(string Path);
public record UserEventRequest(string EventName, string? UserId, string? Properties);
