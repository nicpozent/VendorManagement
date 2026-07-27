using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendorReview.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArchiveOwnerObjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerObjectId",
                table: "ArchivedReviews",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerObjectId",
                table: "ArchivedReviews");
        }
    }
}
