using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspireDemo.Api.Data.Migrations;

/// <summary>
/// Adds audit fields (Description, CreatedAt, UpdatedAt) to Products
/// </summary>
public partial class AddAuditFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "Products",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAt",
            table: "Products",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAt",
            table: "Products",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "Products");
        migrationBuilder.DropColumn(name: "CreatedAt", table: "Products");
        migrationBuilder.DropColumn(name: "Description", table: "Products");
    }
}
