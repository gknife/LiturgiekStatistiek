using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiturgiekStatistiek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTemplateDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultBibleTranslationId",
                table: "ServiceTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBeamerLiturgy",
                table: "ServiceTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBeamerSongs",
                table: "ServiceTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBeamerTexts",
                table: "ServiceTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReadingService",
                table: "ServiceTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "MusicalAccompanimentId",
                table: "ServiceTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplates_DefaultBibleTranslationId",
                table: "ServiceTemplates",
                column: "DefaultBibleTranslationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplates_MusicalAccompanimentId",
                table: "ServiceTemplates",
                column: "MusicalAccompanimentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTemplates_ListItems_DefaultBibleTranslationId",
                table: "ServiceTemplates",
                column: "DefaultBibleTranslationId",
                principalTable: "ListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTemplates_ListItems_MusicalAccompanimentId",
                table: "ServiceTemplates",
                column: "MusicalAccompanimentId",
                principalTable: "ListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTemplates_ListItems_DefaultBibleTranslationId",
                table: "ServiceTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTemplates_ListItems_MusicalAccompanimentId",
                table: "ServiceTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTemplates_DefaultBibleTranslationId",
                table: "ServiceTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTemplates_MusicalAccompanimentId",
                table: "ServiceTemplates");

            migrationBuilder.DropColumn(
                name: "DefaultBibleTranslationId",
                table: "ServiceTemplates");

            migrationBuilder.DropColumn(
                name: "HasBeamerLiturgy",
                table: "ServiceTemplates");

            migrationBuilder.DropColumn(
                name: "HasBeamerSongs",
                table: "ServiceTemplates");

            migrationBuilder.DropColumn(
                name: "HasBeamerTexts",
                table: "ServiceTemplates");

            migrationBuilder.DropColumn(
                name: "IsReadingService",
                table: "ServiceTemplates");

            migrationBuilder.DropColumn(
                name: "MusicalAccompanimentId",
                table: "ServiceTemplates");
        }
    }
}
