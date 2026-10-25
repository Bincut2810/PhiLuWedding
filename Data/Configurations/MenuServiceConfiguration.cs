using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuService"/>.
/// </summary>
public class MenuServiceConfiguration : IEntityTypeConfiguration<MenuService>
{
    public void Configure(EntityTypeBuilder<MenuService> builder)
    {
        builder.ToTable("menu_services");

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

        builder.Property(x => x.PriceType)
            .HasColumnName("price_type")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PriceFrom)
            .HasColumnName("price_from")
            .HasColumnType("numeric(18,0)");

        builder.Property(x => x.PriceTo)
            .HasColumnName("price_to")
            .HasColumnType("numeric(18,0)");

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
            .HasDatabaseName("ix_menu_services_key");

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ix_menu_services_slug");

        builder.HasIndex(x => new { x.VenueScope, x.SortOrder })
            .HasDatabaseName("ix_menu_services_scope_sort");
    }
}
