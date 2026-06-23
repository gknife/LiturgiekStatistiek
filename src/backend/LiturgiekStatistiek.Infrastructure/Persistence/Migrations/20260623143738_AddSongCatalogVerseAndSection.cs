using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiturgiekStatistiek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSongCatalogVerseAndSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Songs_BundleId_Number",
                table: "Songs");

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "Songs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SongCatalogVerses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SongId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongCatalogVerses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongCatalogVerses_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Songs_BundleId_Section_Number",
                table: "Songs",
                columns: new[] { "BundleId", "Section", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SongCatalogVerses_SongId_Number",
                table: "SongCatalogVerses",
                columns: new[] { "SongId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SongCatalogVerses");

            migrationBuilder.DropIndex(
                name: "IX_Songs_BundleId_Section_Number",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "Songs");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_BundleId_Number",
                table: "Songs",
                columns: new[] { "BundleId", "Number" },
                unique: true);
        }
    }
}
