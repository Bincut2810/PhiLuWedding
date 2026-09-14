using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data.Configurations;

/// <summary>
/// Fluent API configuration for the <see cref="MenuComboDish"/> join entity.
/// Uses a composite primary key so the same dish cannot be added twice
/// to the same combo.
/// </summary>
public class MenuComboDishConfiguration : IEntityTypeConfiguration<MenuComboDish>
{
    public void Configure(EntityTypeBuilder<MenuComboDish> builder)
    {
        builder.ToTable("menu_combo_dishes");

        builder.HasKey(x => new { x.MenuComboId, x.DishId });

        builder.Property(x => x.MenuComboId).HasColumnName("menu_combo_id");
        builder.Property(x => x.DishId).HasColumnName("dish_id");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasDefaultValue(1);

        builder.Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.MenuComboId, x.SortOrder })
            .HasDatabaseName("ix_menu_combo_dishes_combo_sort");

        builder.HasIndex(x => x.DishId)
            .HasDatabaseName("ix_menu_combo_dishes_dish");
    }
}
