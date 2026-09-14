using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="Dish"/>.
/// </summary>
public class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    public void Configure(EntityTypeBuilder<Dish> builder)
    {
        builder.ToTable("dishes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

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

        builder.Property(x => x.Ingredients)
            .HasColumnName("ingredients")
            .HasColumnType("text");

        builder.Property(x => x.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(18,2)");

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

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ix_dishes_slug");

        builder.HasIndex(x => new { x.IsPublished, x.SortOrder })
            .HasDatabaseName("ix_dishes_published_sort");

        builder.HasMany(x => x.Images)
            .WithOne(x => x.Dish!)
            .HasForeignKey(x => x.DishId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ComboDishes)
            .WithOne(x => x.Dish!)
            .HasForeignKey(x => x.DishId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
