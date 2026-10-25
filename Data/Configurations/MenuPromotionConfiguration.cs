using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuPromotion"/>.
/// </summary>
public class MenuPromotionConfiguration : IEntityTypeConfiguration<MenuPromotion>
{
    public void Configure(EntityTypeBuilder<MenuPromotion> builder)
    {
        builder.ToTable("menu_promotions");

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

        builder.Property(x => x.RangeLabel)
            .HasColumnName("range_label")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.MinTableCount).HasColumnName("min_table_count");

        builder.Property(x => x.MaxTableCount).HasColumnName("max_table_count");

        builder.Property(x => x.BasePromotionId).HasColumnName("base_promotion_id");
        builder.Property(x => x.IncludesPreviousTier)
            .HasColumnName("includes_previous_tier")
            .HasDefaultValue(true);

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
            .HasDatabaseName("ix_menu_promotions_key");

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ix_menu_promotions_slug");

        builder.HasIndex(x => new { x.MinTableCount, x.MaxTableCount })
            .HasDatabaseName("ix_menu_promotions_table_range");

        // Self reference used to chain inherited benefits.
        builder.HasOne(x => x.BasePromotion)
            .WithMany(x => x.DerivedPromotions)
            .HasForeignKey(x => x.BasePromotionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
