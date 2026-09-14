using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="WeddingHall"/>.
/// </summary>
public class WeddingHallConfiguration : IEntityTypeConfiguration<WeddingHall>
{
    public void Configure(EntityTypeBuilder<WeddingHall> builder)
    {
        builder.ToTable("wedding_halls");

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

        builder.Property(x => x.ShortDescription)
            .HasColumnName("short_description")
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(x => x.CapacityMin).HasColumnName("capacity_min");
        builder.Property(x => x.CapacityMax).HasColumnName("capacity_max");

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
            .HasDatabaseName("ix_wedding_halls_slug");

        builder.HasIndex(x => new { x.IsPublished, x.SortOrder })
            .HasDatabaseName("ix_wedding_halls_published_sort");

        builder.HasMany(x => x.Images)
            .WithOne(x => x.WeddingHall!)
            .HasForeignKey(x => x.WeddingHallId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
