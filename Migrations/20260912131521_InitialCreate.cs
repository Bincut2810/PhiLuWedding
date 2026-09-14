using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PhiluWedding.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_date = table.Column<DateOnly>(type: "date", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    estimated_guest_count = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dishes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    ingredients = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dishes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_sets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    short_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wedding_halls",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    short_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    capacity_min = table.Column<int>(type: "integer", nullable: true),
                    capacity_max = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wedding_halls", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dish_images",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dish_id = table.Column<int>(type: "integer", nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    public_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    alt_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dish_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_dish_images_dishes_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dishes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_combos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    menu_set_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    guest_count = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_combos", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_combos_menu_sets_menu_set_id",
                        column: x => x.menu_set_id,
                        principalTable: "menu_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hall_images",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wedding_hall_id = table.Column<int>(type: "integer", nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    public_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    alt_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hall_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_hall_images_wedding_halls_wedding_hall_id",
                        column: x => x.wedding_hall_id,
                        principalTable: "wedding_halls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_combo_dishes",
                columns: table => new
                {
                    menu_combo_id = table.Column<int>(type: "integer", nullable: false),
                    dish_id = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_combo_dishes", x => new { x.menu_combo_id, x.dish_id });
                    table.ForeignKey(
                        name: "FK_menu_combo_dishes_dishes_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dishes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_menu_combo_dishes_menu_combos_menu_combo_id",
                        column: x => x.menu_combo_id,
                        principalTable: "menu_combos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booking_requests_created_at",
                table: "booking_requests",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_booking_requests_event_date",
                table: "booking_requests",
                column: "event_date");

            migrationBuilder.CreateIndex(
                name: "ix_booking_requests_status",
                table: "booking_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_dish_images_dish_primary",
                table: "dish_images",
                columns: new[] { "dish_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "ix_dish_images_dish_sort",
                table: "dish_images",
                columns: new[] { "dish_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_dishes_published_sort",
                table: "dishes",
                columns: new[] { "is_published", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_dishes_slug",
                table: "dishes",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hall_images_hall_primary",
                table: "hall_images",
                columns: new[] { "wedding_hall_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "ix_hall_images_hall_sort",
                table: "hall_images",
                columns: new[] { "wedding_hall_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_combo_dishes_combo_sort",
                table: "menu_combo_dishes",
                columns: new[] { "menu_combo_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_combo_dishes_dish",
                table: "menu_combo_dishes",
                column: "dish_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_combos_set_published_sort",
                table: "menu_combos",
                columns: new[] { "menu_set_id", "is_published", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_combos_set_slug",
                table: "menu_combos",
                columns: new[] { "menu_set_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_sets_published_sort",
                table: "menu_sets",
                columns: new[] { "is_published", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_sets_slug",
                table: "menu_sets",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wedding_halls_published_sort",
                table: "wedding_halls",
                columns: new[] { "is_published", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_wedding_halls_slug",
                table: "wedding_halls",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_requests");

            migrationBuilder.DropTable(
                name: "dish_images");

            migrationBuilder.DropTable(
                name: "hall_images");

            migrationBuilder.DropTable(
                name: "menu_combo_dishes");

            migrationBuilder.DropTable(
                name: "wedding_halls");

            migrationBuilder.DropTable(
                name: "dishes");

            migrationBuilder.DropTable(
                name: "menu_combos");

            migrationBuilder.DropTable(
                name: "menu_sets");
        }
    }
}
