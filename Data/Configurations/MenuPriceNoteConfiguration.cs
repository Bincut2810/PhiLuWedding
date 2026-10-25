using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuPriceNote"/>.
/// The reusable price-validity / VAT metadata for the future public menu
/// page and admin screen.
/// </summary>
public class MenuPriceNoteConfiguration : IEntityTypeConfiguration<MenuPriceNote>
{
    public void Configure(EntityTypeBuilder<MenuPriceNote> builder)
    {
        builder.ToTable("menu_price_notes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ValidUntil)
            .HasColumnName("valid_until")
            .HasColumnType("date");

        builder.Property(x => x.VatNote)
            .HasColumnName("vat_note")
            .HasMaxLength(500);

        builder.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

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
            .HasDatabaseName("ix_menu_price_notes_key");

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ix_menu_price_notes_slug");
    }
}
