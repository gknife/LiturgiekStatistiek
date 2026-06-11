using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiturgiekStatistiek.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangeHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    PreviousValues = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TitleNl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSystemList = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QueryParameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListItems_ListDefinitions_ListDefinitionId",
                        column: x => x.ListDefinitionId,
                        principalTable: "ListDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Congregations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationDetail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DenominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Modality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Congregations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Congregations_ListItems_DenominationId",
                        column: x => x.DenominationId,
                        principalTable: "ListItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Preachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DenominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    City = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Preachers_ListItems_DenominationId",
                        column: x => x.DenominationId,
                        principalTable: "ListItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Songs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NumberOfVerses = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Songs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Songs_ListItems_BundleId",
                        column: x => x.BundleId,
                        principalTable: "ListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeOfDay = table.Column<int>(type: "int", nullable: false),
                    CongregationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChurchCalendarSundayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BibleTranslationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsReadingService = table.Column<bool>(type: "bit", nullable: false),
                    ReadSermonBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MusicalAccompanimentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HasBeamerLiturgy = table.Column<bool>(type: "bit", nullable: false),
                    HasBeamerTexts = table.Column<bool>(type: "bit", nullable: false),
                    HasBeamerSongs = table.Column<bool>(type: "bit", nullable: false),
                    HasBeamerTextsAndSongs = table.Column<bool>(type: "bit", nullable: false),
                    BroadcastUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SpecialOccasionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SermonText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SermonTheme = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Congregations_CongregationId",
                        column: x => x.CongregationId,
                        principalTable: "Congregations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Services_ListItems_BibleTranslationId",
                        column: x => x.BibleTranslationId,
                        principalTable: "ListItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Services_ListItems_ChurchCalendarSundayId",
                        column: x => x.ChurchCalendarSundayId,
                        principalTable: "ListItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Services_ListItems_MusicalAccompanimentId",
                        column: x => x.MusicalAccompanimentId,
                        principalTable: "ListItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Services_ListItems_SpecialOccasionId",
                        column: x => x.SpecialOccasionId,
                        principalTable: "ListItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Services_Preachers_PreacherId",
                        column: x => x.PreacherId,
                        principalTable: "Preachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBundles",
                columns: table => new
                {
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBundles", x => new { x.ServiceId, x.BundleId });
                    table.ForeignKey(
                        name: "FK_ServiceBundles_ListItems_BundleId",
                        column: x => x.BundleId,
                        principalTable: "ListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceBundles_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    ElementType = table.Column<int>(type: "int", nullable: false),
                    LabelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScriptureReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceElements_ListItems_LabelId",
                        column: x => x.LabelId,
                        principalTable: "ListItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceElements_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceMetadata_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceElementSongs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SongNumber = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceElementSongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceElementSongs_ListItems_BundleId",
                        column: x => x.BundleId,
                        principalTable: "ListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceElementSongs_ServiceElements_ServiceElementId",
                        column: x => x.ServiceElementId,
                        principalTable: "ServiceElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SongVerses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceElementSongId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerseLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongVerses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongVerses_ServiceElementSongs_ServiceElementSongId",
                        column: x => x.ServiceElementSongId,
                        principalTable: "ServiceElementSongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeHistory_ChangedAt",
                table: "ChangeHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeHistory_EntityType_EntityId",
                table: "ChangeHistory",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Congregations_DenominationId",
                table: "Congregations",
                column: "DenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_Slug",
                table: "ContentPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListDefinitions_Name",
                table: "ListDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListItems_ListDefinitionId",
                table: "ListItems",
                column: "ListDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Preachers_DenominationId",
                table: "Preachers",
                column: "DenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBundles_BundleId",
                table: "ServiceBundles",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceElements_LabelId",
                table: "ServiceElements",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceElements_ServiceId_Position",
                table: "ServiceElements",
                columns: new[] { "ServiceId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceElementSongs_BundleId_SongNumber",
                table: "ServiceElementSongs",
                columns: new[] { "BundleId", "SongNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceElementSongs_ServiceElementId",
                table: "ServiceElementSongs",
                column: "ServiceElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMetadata_ServiceId",
                table: "ServiceMetadata",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_BibleTranslationId",
                table: "Services",
                column: "BibleTranslationId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ChurchCalendarSundayId",
                table: "Services",
                column: "ChurchCalendarSundayId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CongregationId",
                table: "Services",
                column: "CongregationId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Date",
                table: "Services",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Services_MusicalAccompanimentId",
                table: "Services",
                column: "MusicalAccompanimentId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_PreacherId",
                table: "Services",
                column: "PreacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_SpecialOccasionId",
                table: "Services",
                column: "SpecialOccasionId");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_BundleId_Number",
                table: "Songs",
                columns: new[] { "BundleId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SongVerses_ServiceElementSongId",
                table: "SongVerses",
                column: "ServiceElementSongId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeHistory");

            migrationBuilder.DropTable(
                name: "ContentPages");

            migrationBuilder.DropTable(
                name: "SavedQueries");

            migrationBuilder.DropTable(
                name: "ServiceBundles");

            migrationBuilder.DropTable(
                name: "ServiceMetadata");

            migrationBuilder.DropTable(
                name: "Songs");

            migrationBuilder.DropTable(
                name: "SongVerses");

            migrationBuilder.DropTable(
                name: "ServiceElementSongs");

            migrationBuilder.DropTable(
                name: "ServiceElements");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Congregations");

            migrationBuilder.DropTable(
                name: "Preachers");

            migrationBuilder.DropTable(
                name: "ListItems");

            migrationBuilder.DropTable(
                name: "ListDefinitions");
        }
    }
}
