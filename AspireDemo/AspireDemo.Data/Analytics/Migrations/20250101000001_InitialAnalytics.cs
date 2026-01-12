using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AspireDemo.Data.Analytics.Migrations;

public partial class InitialAnalytics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PageViews",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                IpAddress = table.Column<string>(type: "text", nullable: true),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PageViews", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserEvents",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                EventName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UserId = table.Column<string>(type: "text", nullable: true),
                Properties = table.Column<string>(type: "jsonb", nullable: true),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserEvents", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PageViews_Timestamp",
            table: "PageViews",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_UserEvents_EventName_Timestamp",
            table: "UserEvents",
            columns: new[] { "EventName", "Timestamp" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserEvents");
        migrationBuilder.DropTable(name: "PageViews");
    }
}
