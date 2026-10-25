using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuPromotionTerm"/> — the shared
/// terms that apply to every promotion tier.
/// </summary>
public class MenuPromotionTermConfiguration : IEntityTypeConfiguration<MenuPromotionTerm>
{
    public void Configure(EntityTypeBuilder<MenuPromotionTerm> builder)
    {
        builder.ToTable("menu_promotion_terms");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

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
            .HasDatabaseName("ix_menu_promotion_terms_key");

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ix_menu_promotion_terms_slug");
    }
}
