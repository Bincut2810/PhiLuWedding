using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiluWedding.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueKeyToWeddingHall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "venue_key",
                table: "wedding_halls",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_wedding_halls_venue_published_sort",
                table: "wedding_halls",
                columns: new[] { "venue_key", "is_published", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_wedding_halls_venue_published_sort",
                table: "wedding_halls");

            migrationBuilder.DropColumn(
                name: "venue_key",
                table: "wedding_halls");
        }
    }
}
