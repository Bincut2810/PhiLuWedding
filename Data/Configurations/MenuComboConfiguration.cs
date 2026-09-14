using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuCombo"/>.
/// </summary>
public class MenuComboConfiguration : IEntityTypeConfiguration<MenuCombo>
{
    public void Configure(EntityTypeBuilder<MenuCombo> builder)
    {
        builder.ToTable("menu_combos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.MenuSetId).HasColumnName("menu_set_id");

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

        builder.Property(x => x.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.GuestCount).HasColumnName("guest_count");

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

        // Slug must be unique within a menu set, not globally.
        builder.HasIndex(x => new { x.MenuSetId, x.Slug })
            .IsUnique()
            .HasDatabaseName("ix_menu_combos_set_slug");

        builder.HasIndex(x => new { x.MenuSetId, x.IsPublished, x.SortOrder })
            .HasDatabaseName("ix_menu_combos_set_published_sort");

        builder.HasMany(x => x.ComboDishes)
            .WithOne(x => x.MenuCombo!)
            .HasForeignKey(x => x.MenuComboId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
