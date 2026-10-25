using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuDepositTerm"/>.
/// Terms are scoped to a venue and, when hall specific, to a stable hall slug.
/// </summary>
public class MenuDepositTermConfiguration : IEntityTypeConfiguration<MenuDepositTerm>
{
    public void Configure(EntityTypeBuilder<MenuDepositTerm> builder)
    {
        builder.ToTable("menu_deposit_terms");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.VenueScope)
            .HasColumnName("venue_scope")
            .HasMaxLength(50)
            .IsRequired();

        // Null for venue-wide terms such as the final-payment policy.
        builder.Property(x => x.HallSlug)
            .HasColumnName("hall_slug")
            .HasMaxLength(200);

        builder.Property(x => x.HallName)
            .HasColumnName("hall_name")
            .HasMaxLength(200);

        builder.Property(x => x.WeddingHallId).HasColumnName("wedding_hall_id");

        builder.Property(x => x.Stage)
            .HasColumnName("stage")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,0)");

        builder.Property(x => x.Description)
            .HasColumnName("description")
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

        // The seed key is the idempotency guard for the seeder.
        builder.HasIndex(x => x.Key)
            .IsUnique()
            .HasDatabaseName("ix_menu_deposit_terms_key");

        // One deposit term per hall and stage.
        builder.HasIndex(x => new { x.VenueScope, x.HallSlug, x.Stage })
            .IsUnique()
            .HasDatabaseName("ix_menu_deposit_terms_scope_hall_stage");

        builder.HasIndex(x => new { x.VenueScope, x.SortOrder })
            .HasDatabaseName("ix_menu_deposit_terms_scope_sort");

        builder.HasOne(x => x.WeddingHall)
            .WithMany()
            .HasForeignKey(x => x.WeddingHallId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
