using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuSet"/>.
/// </summary>
public class MenuSetConfiguration : IEntityTypeConfiguration<MenuSet>
{
    public void Configure(EntityTypeBuilder<MenuSet> builder)
    {
        builder.ToTable("menu_sets");

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
            .HasDatabaseName("ix_menu_sets_slug");

        builder.HasIndex(x => new { x.IsPublished, x.SortOrder })
            .HasDatabaseName("ix_menu_sets_published_sort");

        builder.HasMany(x => x.Combos)
            .WithOne(x => x.MenuSet!)
            .HasForeignKey(x => x.MenuSetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
