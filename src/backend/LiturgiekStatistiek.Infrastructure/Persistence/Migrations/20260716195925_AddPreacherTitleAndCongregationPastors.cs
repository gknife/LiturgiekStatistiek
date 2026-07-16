using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiturgiekStatistiek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreacherTitleAndCongregationPastors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TitleId",
                table: "Preachers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CongregationPreachers",
                columns: table => new
                {
                    CongregationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongregationPreachers", x => new { x.CongregationId, x.PreacherId });
                    table.ForeignKey(
                        name: "FK_CongregationPreachers_Congregations_CongregationId",
                        column: x => x.CongregationId,
                        principalTable: "Congregations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongregationPreachers_Preachers_PreacherId",
                        column: x => x.PreacherId,
                        principalTable: "Preachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Preachers_TitleId",
                table: "Preachers",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_CongregationPreachers_PreacherId",
                table: "CongregationPreachers",
                column: "PreacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Preachers_ListItems_TitleId",
                table: "Preachers",
                column: "TitleId",
                principalTable: "ListItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Preachers_ListItems_TitleId",
                table: "Preachers");

            migrationBuilder.DropTable(
                name: "CongregationPreachers");

            migrationBuilder.DropIndex(
                name: "IX_Preachers_TitleId",
                table: "Preachers");

            migrationBuilder.DropColumn(
                name: "TitleId",
                table: "Preachers");
        }
    }
}
