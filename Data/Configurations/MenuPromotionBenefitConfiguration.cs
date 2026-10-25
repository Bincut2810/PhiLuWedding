using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MenuPromotionBenefit"/>.
/// </summary>
public class MenuPromotionBenefitConfiguration : IEntityTypeConfiguration<MenuPromotionBenefit>
{
    public void Configure(EntityTypeBuilder<MenuPromotionBenefit> builder)
    {
        builder.ToTable("menu_promotion_benefits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.MenuPromotionId).HasColumnName("menu_promotion_id");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(x => x.Text)
            .HasColumnName("text")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(x => new { x.MenuPromotionId, x.SortOrder })
            .HasDatabaseName("ix_menu_promotion_benefits_promotion_sort");

        builder.HasOne(x => x.MenuPromotion)
            .WithMany(x => x.Benefits)
            .HasForeignKey(x => x.MenuPromotionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
