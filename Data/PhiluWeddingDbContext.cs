using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Data;

/// <summary>
/// Application database context for Phì Lũ Wedding.
/// PostgreSQL via Npgsql. All schema rules live in
/// <c>Data/Configurations/*Configuration.cs</c>.
/// </summary>
public class PhiluWeddingDbContext : DbContext
{
    public PhiluWeddingDbContext(DbContextOptions<PhiluWeddingDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeddingHall> WeddingHalls => Set<WeddingHall>();
    public DbSet<HallImage> HallImages => Set<HallImage>();

    public DbSet<MenuSet> MenuSets => Set<MenuSet>();
    public DbSet<MenuCombo> MenuCombos => Set<MenuCombo>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<MenuComboDish> MenuComboDishes => Set<MenuComboDish>();
    public DbSet<DishImage> DishImages => Set<DishImage>();

    // Wedding-menu price tiers (one shared price per tier, both venues) and
    // the additional menu sections: services, beverages, appetizer nuts,
    // promotions, deposit terms and price metadata.
    public DbSet<MenuTier> MenuTiers => Set<MenuTier>();
    public DbSet<MenuService> MenuServices => Set<MenuService>();
    public DbSet<MenuBeverage> MenuBeverages => Set<MenuBeverage>();
    public DbSet<MenuAppetizerNut> MenuAppetizerNuts => Set<MenuAppetizerNut>();
    public DbSet<MenuPromotion> MenuPromotions => Set<MenuPromotion>();
    public DbSet<MenuPromotionBenefit> MenuPromotionBenefits => Set<MenuPromotionBenefit>();
    public DbSet<MenuPromotionTerm> MenuPromotionTerms => Set<MenuPromotionTerm>();
    public DbSet<MenuDepositTerm> MenuDepositTerms => Set<MenuDepositTerm>();
    public DbSet<MenuPriceNote> MenuPriceNotes => Set<MenuPriceNote>();

    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply every IEntityTypeConfiguration<T> in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
