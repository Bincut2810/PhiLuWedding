using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuBeverage"/>.
/// </summary>
public class MenuBeverageConfiguration : IEntityTypeConfiguration<MenuBeverage>
{
    public void Configure(EntityTypeBuilder<MenuBeverage> builder)
    {
        builder.ToTable("menu_beverages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.PriceBelowThreshold)
            .HasColumnName("price_below_threshold")
            .HasColumnType("numeric(18,0)");

        builder.Property(x => x.PriceAboveThreshold)
            .HasColumnName("price_above_threshold")
            .HasColumnType("numeric(18,0)");

        builder.Property(x => x.ThresholdTableCount)
            .HasColumnName("threshold_table_count")
            .HasDefaultValue(25);

        builder.Property(x => x.ThresholdNote)
            .HasColumnName("threshold_note")
            .HasMaxLength(500);

        builder.Property(x => x.Unit)
            .HasColumnName("unit")
            .HasMaxLength(100);

        builder.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(x => x.VenueScope)
            .HasColumnName("venue_scope")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IsPublished)
            .HasColumnName("is_published")
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.Key)
            .IsUnique()
            .HasDatabaseName("ix_menu_beverages_key");

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ix_menu_beverages_slug");

        builder.HasIndex(x => new { x.VenueScope, x.SortOrder })
            .HasDatabaseName("ix_menu_beverages_scope_sort");
    }
}
