using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuTier"/>.
/// One row per wedding-menu price tier; the price is shared by both venues.
/// </summary>
public class MenuTierConfiguration : IEntityTypeConfiguration<MenuTier>
{
    public void Configure(EntityTypeBuilder<MenuTier> builder)
    {
        builder.ToTable("menu_tiers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        // VND has no minor unit, so the price is stored as an exact integer
        // amount. numeric(18,0) keeps it exact and avoids floating point.
        builder.Property(x => x.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(18,0)");

        builder.Property(x => x.GuestsPerTable)
            .HasColumnName("guests_per_table")
            .HasDefaultValue(10);

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
            .HasDatabaseName("ix_menu_tiers_key");

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ix_menu_tiers_slug");

        builder.HasIndex(x => new { x.VenueScope, x.SortOrder })
            .HasDatabaseName("ix_menu_tiers_scope_sort");

        // A tier is never deleted implicitly: options reference it.
        builder.HasMany(x => x.Options)
            .WithOne(x => x.MenuTier!)
            .HasForeignKey(x => x.MenuTierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
