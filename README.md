# Aspire Demo (asp-explore)

This repo is a small .NET Aspire sample that hosts a Minimal API, two PostgreSQL databases,
and a manual-start worker. It exists mainly to show two EF Core migration patterns side by side.

## What it does
- Serves a products API backed by `appdb`.
- Tracks analytics (page views and events) in `analyticsdb`.
- Demonstrates migrations:
  - Pattern 1: dedicated migrator project for `appdb`.
  - Pattern 2: AppHost hosted service for `analyticsdb`.
- Includes a manual-start worker that cleans up old analytics data.

## Architecture (ASCII)

Resource dependency map:
```text
AppHost (AspireDemo.AppHost)
  |
  |-- appdb (Postgres) ----------> API (products)
  |-- analyticsdb (Postgres) -----> API (analytics)
  |-- analyticsdb (Postgres) -----> Worker (manual start)
  |-- DbMigrator (one-shot) ------> appdb (migrations + dev seed)
  |-- AppHost HostedService ------> analyticsdb (migrations)
```

Migration patterns:
```text
Pattern 1: Dedicated migrator
DbMigrator (one-shot) ---> appdb

Pattern 2: Migrations from AppHost
AppHost HostedService ---> analyticsdb
```

## Projects
- `AspireDemo/AspireDemo.AppHost` - orchestration and analytics migrations.
- `AspireDemo/AspireDemo.Api` - Minimal API for products and analytics.
- `AspireDemo/AspireDemo.DbMigrator` - one-shot migrations for `appdb`; seeds demo data in Development.
- `AspireDemo/AspireDemo.Worker` - manual-start cleanup worker for `analyticsdb`.
- `AspireDemo/AspireDemo.Data` - EF Core DbContexts and migrations.

## Run locally
```bash
cd AspireDemo/AspireDemo.AppHost
aspire run
```

Notes:
- Swagger UI is enabled in Development.
- Start the worker manually via the Aspire dashboard.

## API surface (high level)
- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `DELETE /api/products/{id}`
- `POST /api/analytics/pageview`
- `POST /api/analytics/event`
- `GET /api/analytics/pageviews`
- `GET /api/analytics/events?eventName=...`
