using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PhiluWedding.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuCatalogFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "venue_scope",
                table: "menu_sets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Both");

            migrationBuilder.AddColumn<int>(
                name: "menu_tier_id",
                table: "menu_combos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "option_code",
                table: "menu_combos",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "option_sort_order",
                table: "menu_combos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "menu_appetizer_nuts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    price_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    venue_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_appetizer_nuts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_beverages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    price_below_threshold = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    price_above_threshold = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    threshold_table_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 25),
                    threshold_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    venue_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_beverages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_deposit_terms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    venue_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hall_slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    hall_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    wedding_hall_id = table.Column<int>(type: "integer", nullable: true),
                    stage = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_deposit_terms", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_deposit_terms_wedding_halls_wedding_hall_id",
                        column: x => x.wedding_hall_id,
                        principalTable: "wedding_halls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "menu_price_notes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    vat_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_price_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_promotion_terms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_promotion_terms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_promotions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    range_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    min_table_count = table.Column<int>(type: "integer", nullable: true),
                    max_table_count = table.Column<int>(type: "integer", nullable: true),
                    base_promotion_id = table.Column<int>(type: "integer", nullable: true),
                    includes_previous_tier = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    venue_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_promotions", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_promotions_menu_promotions_base_promotion_id",
                        column: x => x.base_promotion_id,
                        principalTable: "menu_promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_services",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    price_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    price_from = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    price_to = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    venue_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_tiers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    guests_per_table = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    venue_scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_tiers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_promotion_benefits",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    menu_promotion_id = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_promotion_benefits", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_promotion_benefits_menu_promotions_menu_promotion_id",
                        column: x => x.menu_promotion_id,
                        principalTable: "menu_promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_menu_sets_scope_sort",
                table: "menu_sets",
                columns: new[] { "venue_scope", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_combos_tier_option",
                table: "menu_combos",
                columns: new[] { "menu_tier_id", "option_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_combos_tier_option_sort",
                table: "menu_combos",
                columns: new[] { "menu_tier_id", "option_sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_appetizer_nuts_key",
                table: "menu_appetizer_nuts",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_appetizer_nuts_scope_sort",
                table: "menu_appetizer_nuts",
                columns: new[] { "venue_scope", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_appetizer_nuts_slug",
                table: "menu_appetizer_nuts",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_beverages_key",
                table: "menu_beverages",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_beverages_scope_sort",
                table: "menu_beverages",
                columns: new[] { "venue_scope", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_beverages_slug",
                table: "menu_beverages",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_deposit_terms_key",
                table: "menu_deposit_terms",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_deposit_terms_scope_hall_stage",
                table: "menu_deposit_terms",
                columns: new[] { "venue_scope", "hall_slug", "stage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_deposit_terms_scope_sort",
                table: "menu_deposit_terms",
                columns: new[] { "venue_scope", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_menu_deposit_terms_wedding_hall_id",
                table: "menu_deposit_terms",
                column: "wedding_hall_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_price_notes_key",
                table: "menu_price_notes",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_price_notes_slug",
                table: "menu_price_notes",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_promotion_benefits_promotion_sort",
                table: "menu_promotion_benefits",
                columns: new[] { "menu_promotion_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_promotion_terms_key",
                table: "menu_promotion_terms",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_promotion_terms_slug",
                table: "menu_promotion_terms",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_menu_promotions_base_promotion_id",
                table: "menu_promotions",
                column: "base_promotion_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_promotions_key",
                table: "menu_promotions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_promotions_slug",
                table: "menu_promotions",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_promotions_table_range",
                table: "menu_promotions",
                columns: new[] { "min_table_count", "max_table_count" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_services_key",
                table: "menu_services",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_services_scope_sort",
                table: "menu_services",
                columns: new[] { "venue_scope", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_services_slug",
                table: "menu_services",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_tiers_key",
                table: "menu_tiers",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_tiers_scope_sort",
                table: "menu_tiers",
                columns: new[] { "venue_scope", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_tiers_slug",
                table: "menu_tiers",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_menu_combos_menu_tiers_menu_tier_id",
                table: "menu_combos",
                column: "menu_tier_id",
                principalTable: "menu_tiers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_menu_combos_menu_tiers_menu_tier_id",
                table: "menu_combos");

            migrationBuilder.DropTable(
                name: "menu_appetizer_nuts");

            migrationBuilder.DropTable(
                name: "menu_beverages");

            migrationBuilder.DropTable(
                name: "menu_deposit_terms");

            migrationBuilder.DropTable(
                name: "menu_price_notes");

            migrationBuilder.DropTable(
                name: "menu_promotion_benefits");

            migrationBuilder.DropTable(
                name: "menu_promotion_terms");

            migrationBuilder.DropTable(
                name: "menu_services");

            migrationBuilder.DropTable(
                name: "menu_tiers");

            migrationBuilder.DropTable(
                name: "menu_promotions");

            migrationBuilder.DropIndex(
                name: "ix_menu_sets_scope_sort",
                table: "menu_sets");

            migrationBuilder.DropIndex(
                name: "ix_menu_combos_tier_option",
                table: "menu_combos");

            migrationBuilder.DropIndex(
                name: "ix_menu_combos_tier_option_sort",
                table: "menu_combos");

            migrationBuilder.DropColumn(
                name: "venue_scope",
                table: "menu_sets");

            migrationBuilder.DropColumn(
                name: "menu_tier_id",
                table: "menu_combos");

            migrationBuilder.DropColumn(
                name: "option_code",
                table: "menu_combos");

            migrationBuilder.DropColumn(
                name: "option_sort_order",
                table: "menu_combos");
        }
    }
}
