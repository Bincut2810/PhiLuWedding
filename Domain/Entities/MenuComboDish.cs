namespace PhiluWedding.Domain.Entities;

/// <summary>
/// Join entity linking <see cref="MenuCombo"/> to <see cref="Dish"/> in a many-to-many
/// relationship. A composite primary key prevents the same dish from being added
/// twice to the same combo.
/// </summary>
public class MenuComboDish
{
    public int MenuComboId { get; set; }

    public int DishId { get; set; }

    public int SortOrder { get; set; }

    /// <summary>How many servings of this dish are included in the combo.</summary>
    public int Quantity { get; set; } = 1;

    public string? Note { get; set; }

    public MenuCombo? MenuCombo { get; set; }

    public Dish? Dish { get; set; }
}
