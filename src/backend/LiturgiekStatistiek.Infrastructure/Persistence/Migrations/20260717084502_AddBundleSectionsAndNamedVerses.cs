using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiturgiekStatistiek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBundleSectionsAndNamedVerses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SongCatalogVerses_SongId_Number",
                table: "SongCatalogVerses");

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "SongCatalogVerses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "SongCatalogVerses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill SortOrder for existing rows before the unique (SongId, SortOrder)
            // index is created. The previous unique key was (SongId, Number), so seeding
            // SortOrder from Number preserves per-song uniqueness and ordering.
            migrationBuilder.Sql("UPDATE SongCatalogVerses SET SortOrder = Number;");

            migrationBuilder.CreateTable(
                name: "BundleSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BundleSections_ListItems_BundleId",
                        column: x => x.BundleId,
                        principalTable: "ListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SongCatalogVerses_SongId_SortOrder",
                table: "SongCatalogVerses",
                columns: new[] { "SongId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BundleSections_BundleId_Value",
                table: "BundleSections",
                columns: new[] { "BundleId", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BundleSections");

            migrationBuilder.DropIndex(
                name: "IX_SongCatalogVerses_SongId_SortOrder",
                table: "SongCatalogVerses");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "SongCatalogVerses");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "SongCatalogVerses");

            migrationBuilder.CreateIndex(
                name: "IX_SongCatalogVerses_SongId_Number",
                table: "SongCatalogVerses",
                columns: new[] { "SongId", "Number" },
                unique: true);
        }
    }
}
