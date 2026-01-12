using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspireDemo.Api.Data.Migrations;

/// <summary>
/// Seeds reference/lookup data (Categories) that is required for the application to function.
/// This data is stable, versioned, and runs exactly once per environment.
/// </summary>
public partial class SeedReferenceData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "Categories",
            columns: new[] { "Id", "Name", "Description" },
            values: new object[,]
            {
                { 1, "Electronics", "Electronic devices and gadgets" },
                { 2, "Clothing", "Apparel and fashion items" },
                { 3, "Books", "Physical and digital books" },
                { 4, "Home & Garden", "Home improvement and garden supplies" },
                { 5, "Sports", "Sports equipment and accessories" }
            });

        // Reset the sequence to continue after seeded IDs
        migrationBuilder.Sql("SELECT setval('\"Categories_Id_seq\"', 5, true);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "Categories",
            keyColumn: "Id",
            keyValues: new object[] { 1, 2, 3, 4, 5 });
    }
}
