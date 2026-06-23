using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiturgiekStatistiek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceElementSongSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceElementSongs_BundleId_SongNumber",
                table: "ServiceElementSongs");

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "ServiceElementSongs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceElementSongs_BundleId_Section_SongNumber",
                table: "ServiceElementSongs",
                columns: new[] { "BundleId", "Section", "SongNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceElementSongs_BundleId_Section_SongNumber",
                table: "ServiceElementSongs");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "ServiceElementSongs");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceElementSongs_BundleId_SongNumber",
                table: "ServiceElementSongs",
                columns: new[] { "BundleId", "SongNumber" });
        }
    }
}
