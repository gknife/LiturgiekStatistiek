using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiturgiekStatistiek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettingsRecentSearchesBibleAndSermonRefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BibleBooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Testament = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChapterCount = table.Column<int>(type: "int", nullable: false),
                    VerseCountsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecentSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QueryText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentSearches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BibleBookTranslationNames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BibleBookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TranslationAbbreviation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleBookTranslationNames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BibleBookTranslationNames_BibleBooks_BibleBookId",
                        column: x => x.BibleBookId,
                        principalTable: "BibleBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SermonTextReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    BibleBookId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Chapter = table.Column<int>(type: "int", nullable: true),
                    VerseStart = table.Column<int>(type: "int", nullable: true),
                    VerseEnd = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SermonTextReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SermonTextReferences_BibleBooks_BibleBookId",
                        column: x => x.BibleBookId,
                        principalTable: "BibleBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SermonTextReferences_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BibleBooks_Ordinal",
                table: "BibleBooks",
                column: "Ordinal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibleBookTranslationNames_BibleBookId",
                table: "BibleBookTranslationNames",
                column: "BibleBookId");

            migrationBuilder.CreateIndex(
                name: "IX_RecentSearches_UserId_CreatedAt",
                table: "RecentSearches",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SermonTextReferences_BibleBookId",
                table: "SermonTextReferences",
                column: "BibleBookId");

            migrationBuilder.CreateIndex(
                name: "IX_SermonTextReferences_ServiceId",
                table: "SermonTextReferences",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BibleBookTranslationNames");

            migrationBuilder.DropTable(
                name: "RecentSearches");

            migrationBuilder.DropTable(
                name: "SermonTextReferences");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "BibleBooks");
        }
    }
}
