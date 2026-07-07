using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiturgiekStatistiek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplatesReadingRefsPerformerBeurtzangStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- New columns on ServiceElements (added first so data can be migrated) ---
            migrationBuilder.AddColumn<Guid>(
                name: "BibleTranslationId",
                table: "ServiceElements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBeurtzang",
                table: "ServiceElements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PerformerId",
                table: "ServiceElements",
                type: "uniqueidentifier",
                nullable: true);

            // --- Data migration: copy the old service-wide translation onto each
            // reading onderdeel (ElementType 2 = Reading) before the column is dropped. ---
            migrationBuilder.Sql(@"
                UPDATE e
                SET e.BibleTranslationId = s.BibleTranslationId
                FROM ServiceElements e
                INNER JOIN Services s ON s.Id = e.ServiceId
                WHERE e.ElementType = 2 AND s.BibleTranslationId IS NOT NULL;");

            // --- Status on Services; backfill existing rows to Gepubliceerd (1). ---
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Services",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Services SET Status = 1;");

            // --- Drop the old service-wide translation column ---
            migrationBuilder.DropForeignKey(
                name: "FK_Services_ListItems_BibleTranslationId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_BibleTranslationId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "BibleTranslationId",
                table: "Services");

            migrationBuilder.CreateTable(
                name: "ReadingReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    BibleBookId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Chapter = table.Column<int>(type: "int", nullable: true),
                    VerseStart = table.Column<int>(type: "int", nullable: true),
                    VerseEnd = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingReferences_BibleBooks_BibleBookId",
                        column: x => x.BibleBookId,
                        principalTable: "BibleBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReadingReferences_ServiceElements_ServiceElementId",
                        column: x => x.ServiceElementId,
                        principalTable: "ServiceElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DenominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CongregationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeOfDay = table.Column<int>(type: "int", nullable: true),
                    OccasionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceTemplates_Congregations_CongregationId",
                        column: x => x.CongregationId,
                        principalTable: "Congregations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceTemplates_ListItems_DenominationId",
                        column: x => x.DenominationId,
                        principalTable: "ListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceTemplates_ListItems_OccasionId",
                        column: x => x.OccasionId,
                        principalTable: "ListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTemplateElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    ElementType = table.Column<int>(type: "int", nullable: false),
                    LabelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PerformerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsBeurtzang = table.Column<bool>(type: "bit", nullable: false),
                    FixedScriptureReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTemplateElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceTemplateElements_ListItems_LabelId",
                        column: x => x.LabelId,
                        principalTable: "ListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceTemplateElements_ListItems_PerformerId",
                        column: x => x.PerformerId,
                        principalTable: "ListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceTemplateElements_ServiceTemplates_ServiceTemplateId",
                        column: x => x.ServiceTemplateId,
                        principalTable: "ServiceTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_Status",
                table: "Services",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceElements_BibleTranslationId",
                table: "ServiceElements",
                column: "BibleTranslationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceElements_PerformerId",
                table: "ServiceElements",
                column: "PerformerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingReferences_BibleBookId",
                table: "ReadingReferences",
                column: "BibleBookId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingReferences_ServiceElementId_Position",
                table: "ReadingReferences",
                columns: new[] { "ServiceElementId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplateElements_LabelId",
                table: "ServiceTemplateElements",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplateElements_PerformerId",
                table: "ServiceTemplateElements",
                column: "PerformerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplateElements_ServiceTemplateId_Position",
                table: "ServiceTemplateElements",
                columns: new[] { "ServiceTemplateId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplates_CongregationId",
                table: "ServiceTemplates",
                column: "CongregationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplates_DenominationId",
                table: "ServiceTemplates",
                column: "DenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplates_OccasionId",
                table: "ServiceTemplates",
                column: "OccasionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceElements_ListItems_BibleTranslationId",
                table: "ServiceElements",
                column: "BibleTranslationId",
                principalTable: "ListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceElements_ListItems_PerformerId",
                table: "ServiceElements",
                column: "PerformerId",
                principalTable: "ListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceElements_ListItems_BibleTranslationId",
                table: "ServiceElements");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceElements_ListItems_PerformerId",
                table: "ServiceElements");

            migrationBuilder.DropTable(
                name: "ReadingReferences");

            migrationBuilder.DropTable(
                name: "ServiceTemplateElements");

            migrationBuilder.DropTable(
                name: "ServiceTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Services_Status",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_ServiceElements_BibleTranslationId",
                table: "ServiceElements");

            migrationBuilder.DropIndex(
                name: "IX_ServiceElements_PerformerId",
                table: "ServiceElements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "BibleTranslationId",
                table: "ServiceElements");

            migrationBuilder.DropColumn(
                name: "IsBeurtzang",
                table: "ServiceElements");

            migrationBuilder.DropColumn(
                name: "PerformerId",
                table: "ServiceElements");

            migrationBuilder.AddColumn<Guid>(
                name: "BibleTranslationId",
                table: "Services",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_BibleTranslationId",
                table: "Services",
                column: "BibleTranslationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_ListItems_BibleTranslationId",
                table: "Services",
                column: "BibleTranslationId",
                principalTable: "ListItems",
                principalColumn: "Id");
        }
    }
}
