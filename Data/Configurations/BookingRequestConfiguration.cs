using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;
using PhiluWedding.Domain.Enums;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="BookingRequest"/>.
/// </summary>
public class BookingRequestConfiguration : IEntityTypeConfiguration<BookingRequest>
{
    public void Configure(EntityTypeBuilder<BookingRequest> builder)
    {
        builder.ToTable("booking_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EventDate)
            .HasColumnName("event_date")
            .HasColumnType("date");

        builder.Property(x => x.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(200);

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EstimatedGuestCount).HasColumnName("estimated_guest_count");

        builder.Property(x => x.Note)
            .HasColumnName("note")
            .HasColumnType("text");

        builder.Property(x => x.PreferredHallId)
            .HasColumnName("preferred_hall_id");

        // Nullable FK to wedding_halls. Restrict mirrors the existing
        // hall_images FK so an unpublished / deleted hall cannot leave
        // dangling request rows behind.
        builder.HasOne(x => x.PreferredHall)
            .WithMany()
            .HasForeignKey(x => x.PreferredHallId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PreferredHallId)
            .HasDatabaseName("ix_booking_requests_preferred_hall_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .HasDefaultValue(BookingStatus.New);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_booking_requests_status");

        builder.HasIndex(x => x.EventDate)
            .HasDatabaseName("ix_booking_requests_event_date");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_booking_requests_created_at");
    }
}
