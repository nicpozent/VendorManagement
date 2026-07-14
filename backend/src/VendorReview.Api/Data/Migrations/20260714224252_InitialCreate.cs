using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendorReview.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntraObjectId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    SourceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ImportedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchivedReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Verdict = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    FinishedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    MemoMarkdown = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IncludedSectionIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Nda = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LastReview = table.Column<DateOnly>(type: "date", nullable: true),
                    RejectedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RejectedReason = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerObjectId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OwnerEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    ReviewRef = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RawPitch = table.Column<string>(type: "text", nullable: false),
                    NdaContactName = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    NdaContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Nda = table.Column<string>(type: "text", nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApproverObjectIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastReminderUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Rule = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Weight = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Policies_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Weight = table.Column<string>(type: "text", nullable: false),
                    Selectable = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionItems_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Resolved = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenQuestions_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewSectionScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewSectionScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewSectionScores_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewItemScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewSectionScoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Weight = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Mitigation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewItemScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewItemScores_ReviewSectionScores_ReviewSectionScoreId",
                        column: x => x.ReviewSectionScoreId,
                        principalTable: "ReviewSectionScores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_EntraObjectId",
                table: "AppUsers",
                column: "EntraObjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenQuestions_ReviewId",
                table: "OpenQuestions",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_SectionId",
                table: "Policies",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewItemScores_ReviewSectionScoreId",
                table: "ReviewItemScores",
                column: "ReviewSectionScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_EntityId",
                table: "Reviews",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSectionScores_ReviewId",
                table: "ReviewSectionScores",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionItems_SectionId",
                table: "SectionItems",
                column: "SectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "ArchivedReviews");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "OpenQuestions");

            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.DropTable(
                name: "ReviewItemScores");

            migrationBuilder.DropTable(
                name: "SectionItems");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "ReviewSectionScores");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Entities");
        }
    }
}
