using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhiluWedding.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredHallToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "preferred_hall_id",
                table: "booking_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_requests_preferred_hall_id",
                table: "booking_requests",
                column: "preferred_hall_id");

            migrationBuilder.AddForeignKey(
                name: "FK_booking_requests_wedding_halls_preferred_hall_id",
                table: "booking_requests",
                column: "preferred_hall_id",
                principalTable: "wedding_halls",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_booking_requests_wedding_halls_preferred_hall_id",
                table: "booking_requests");

            migrationBuilder.DropIndex(
                name: "ix_booking_requests_preferred_hall_id",
                table: "booking_requests");

            migrationBuilder.DropColumn(
                name: "preferred_hall_id",
                table: "booking_requests");
        }
    }
}
