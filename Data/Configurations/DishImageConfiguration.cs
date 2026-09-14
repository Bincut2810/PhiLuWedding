using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="DishImage"/>.
/// </summary>
public class DishImageConfiguration : IEntityTypeConfiguration<DishImage>
{
    public void Configure(EntityTypeBuilder<DishImage> builder)
    {
        builder.ToTable("dish_images");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.DishId).HasColumnName("dish_id");

        builder.Property(x => x.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.PublicId)
            .HasColumnName("public_id")
            .HasMaxLength(200);

        builder.Property(x => x.AltText)
            .HasColumnName("alt_text")
            .HasMaxLength(500);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(x => x.IsPrimary)
            .HasColumnName("is_primary")
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.DishId, x.SortOrder })
            .HasDatabaseName("ix_dish_images_dish_sort");

        builder.HasIndex(x => new { x.DishId, x.IsPrimary })
            .HasDatabaseName("ix_dish_images_dish_primary");
    }
}
