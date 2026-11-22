using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HauntedInventory.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReorderLevel",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Items",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            // Backfill existing items with Sku = 'ITEM-' || Id before creating unique index
            migrationBuilder.Sql("UPDATE Items SET Sku = 'ITEM-' || Id WHERE Sku = '' OR Sku IS NULL");

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Delta = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ResultingQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Sku",
                table: "Items",
                column: "Sku",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Items_Quantity",
                table: "Items",
                sql: "Quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Items_ReorderLevel",
                table: "Items",
                sql: "ReorderLevel >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ItemId_RecordedAt",
                table: "StockMovements",
                columns: new[] { "ItemId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Items_Sku",
                table: "Items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Items_Quantity",
                table: "Items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Items_ReorderLevel",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Items");
        }
    }
}
