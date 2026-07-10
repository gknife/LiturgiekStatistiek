using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiturgiekStatistiek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSongSungInFullAndTemplateDefaultBundle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultSongBundleId",
                table: "ServiceTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SungInFull",
                table: "ServiceElementSongs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplates_DefaultSongBundleId",
                table: "ServiceTemplates",
                column: "DefaultSongBundleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTemplates_ListItems_DefaultSongBundleId",
                table: "ServiceTemplates",
                column: "DefaultSongBundleId",
                principalTable: "ListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTemplates_ListItems_DefaultSongBundleId",
                table: "ServiceTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTemplates_DefaultSongBundleId",
                table: "ServiceTemplates");

            migrationBuilder.DropColumn(
                name: "DefaultSongBundleId",
                table: "ServiceTemplates");

            migrationBuilder.DropColumn(
                name: "SungInFull",
                table: "ServiceElementSongs");
        }
    }
}
