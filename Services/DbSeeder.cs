using Microsoft.EntityFrameworkCore;
using PhiluWedding.Data;
using PhiluWedding.Domain.Entities;

namespace PhiluWedding.Services;

/// <summary>
/// Inserts minimal, clearly-marked demo content for local development.
///
/// Safety rules:
///   * Every demo name uses the "DEMO" prefix so it can never be confused
///     with real Phì Lũ business information.
///   * Capacities, prices, and guest counts are <c>null</c> because no real
///     business data exists yet — a null value makes it visually obvious
///     that no number should be displayed.
///   * No fake booking requests, no fake customer information, no fake
///     Cloudinary identifiers are inserted.
///
/// Schema responsibility:
///   The seeder does NOT create the schema. Callers must apply EF Core
///   migrations first (e.g. <c>dotnet ef database update</c> or
///   <c>await db.Database.MigrateAsync()</c>) before invoking this class.
///
/// Idempotency:
///   The seeder checks each entity table individually, so a partial previous
///   run will not block the rest of the data from being inserted.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(PhiluWeddingDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await SeedHallsAsync(db, now, logger, cancellationToken);
        await SeedMenuSetAsync(db, now, logger, cancellationToken);
        await SeedDishesAsync(db, now, logger, cancellationToken);
        await SeedCombosAsync(db, now, logger, cancellationToken);

        logger.LogInformation("DbSeeder: finished.");
    }

    private static async Task SeedHallsAsync(PhiluWeddingDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        if (await db.WeddingHalls.AnyAsync(ct))
        {
            logger.LogInformation("DbSeeder: halls already exist, skipping.");
            return;
        }

        var halls = new List<WeddingHall>
        {
            new()
            {
                Name = "SẢNH DEMO 01",
                Slug = "sanh-demo-01",
                ShortDescription = "Không gian demo — sẽ được thay bằng thông tin thực.",
                Description = "Mô tả demo cho sảnh tiệc số 01. Nội dung này chỉ dùng cho phát triển.",
                CapacityMin = null,
                CapacityMax = null,
                SortOrder = 10,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "SẢNH DEMO 02",
                Slug = "sanh-demo-02",
                ShortDescription = "Không gian demo — sẽ được thay bằng thông tin thực.",
                Description = "Mô tả demo cho sảnh tiệc số 02. Nội dung này chỉ dùng cho phát triển.",
                CapacityMin = null,
                CapacityMax = null,
                SortOrder = 20,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "SẢNH DEMO 03",
                Slug = "sanh-demo-03",
                ShortDescription = "Không gian demo — sẽ được thay bằng thông tin thực.",
                Description = "Mô tả demo cho sảnh tiệc số 03. Nội dung này chỉ dùng cho phát triển.",
                CapacityMin = null,
                CapacityMax = null,
                SortOrder = 30,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "SẢNH DEMO 04",
                Slug = "sanh-demo-04",
                ShortDescription = "Không gian demo — sẽ được thay bằng thông tin thực.",
                Description = "Mô tả demo cho sảnh tiệc số 04. Nội dung này chỉ dùng cho phát triển.",
                CapacityMin = null,
                CapacityMax = null,
                SortOrder = 40,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        db.WeddingHalls.AddRange(halls);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("DbSeeder: inserted {Count} demo halls.", halls.Count);
    }

    private static async Task SeedMenuSetAsync(PhiluWeddingDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        if (await db.MenuSets.AnyAsync(ct))
        {
            logger.LogInformation("DbSeeder: menu sets already exist, skipping.");
            return;
        }

        var menuSet = new MenuSet
        {
            Name = "MENU DEMO",
            Slug = "menu-demo",
            ShortDescription = "Bộ thực đơn demo cho phát triển.",
            Description = "Mô tả demo. Bộ thực đơn này không phải nội dung thực của Phì Lũ.",
            SortOrder = 10,
            IsPublished = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MenuSets.Add(menuSet);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("DbSeeder: inserted 1 demo menu set.");
    }

    private static async Task SeedDishesAsync(PhiluWeddingDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        if (await db.Dishes.AnyAsync(ct))
        {
            logger.LogInformation("DbSeeder: dishes already exist, skipping.");
            return;
        }

        var dishes = new List<Dish>
        {
            new()
            {
                Name = "MÓN DEMO 01",
                Slug = "mon-demo-01",
                Description = "Mô tả demo cho món ăn 01.",
                Ingredients = "Nguyên liệu demo — sẽ được cập nhật.",
                Price = null,
                SortOrder = 10,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "MÓN DEMO 02",
                Slug = "mon-demo-02",
                Description = "Mô tả demo cho món ăn 02.",
                Ingredients = "Nguyên liệu demo — sẽ được cập nhật.",
                Price = null,
                SortOrder = 20,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "MÓN DEMO 03",
                Slug = "mon-demo-03",
                Description = "Mô tả demo cho món ăn 03.",
                Ingredients = "Nguyên liệu demo — sẽ được cập nhật.",
                Price = null,
                SortOrder = 30,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "MÓN DEMO 04",
                Slug = "mon-demo-04",
                Description = "Mô tả demo cho món ăn 04.",
                Ingredients = "Nguyên liệu demo — sẽ được cập nhật.",
                Price = null,
                SortOrder = 40,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };
        db.Dishes.AddRange(dishes);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("DbSeeder: inserted {Count} demo dishes.", dishes.Count);
    }

    private static async Task SeedCombosAsync(PhiluWeddingDbContext db, DateTime now, ILogger logger, CancellationToken ct)
    {
        if (await db.MenuCombos.AnyAsync(ct))
        {
            logger.LogInformation("DbSeeder: menu combos already exist, skipping.");
            return;
        }

        var menuSet = await db.MenuSets.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (menuSet is null)
        {
            logger.LogInformation("DbSeeder: no menu set exists; cannot seed combos. Skipping.");
            return;
        }

        var dishes = await db.Dishes.OrderBy(x => x.Id).Take(4).ToListAsync(ct);
        if (dishes.Count < 4)
        {
            logger.LogInformation("DbSeeder: fewer than 4 dishes exist; cannot seed combos. Skipping.");
            return;
        }

        var combo1 = new MenuCombo
        {
            MenuSetId = menuSet.Id,
            Name = "COMBO DEMO 01",
            Slug = "combo-demo-01",
            Description = "Combo demo — sẽ được thay bằng nội dung thực.",
            Price = null,
            GuestCount = null,
            SortOrder = 10,
            IsPublished = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var combo2 = new MenuCombo
        {
            MenuSetId = menuSet.Id,
            Name = "COMBO DEMO 02",
            Slug = "combo-demo-02",
            Description = "Combo demo — sẽ được thay bằng nội dung thực.",
            Price = null,
            GuestCount = null,
            SortOrder = 20,
            IsPublished = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MenuCombos.AddRange(combo1, combo2);
        await db.SaveChangesAsync(ct);

        var links = new List<MenuComboDish>
        {
            new() { MenuComboId = combo1.Id, DishId = dishes[0].Id, SortOrder = 10, Quantity = 1 },
            new() { MenuComboId = combo1.Id, DishId = dishes[1].Id, SortOrder = 20, Quantity = 1 },
            new() { MenuComboId = combo1.Id, DishId = dishes[2].Id, SortOrder = 30, Quantity = 1 },
            new() { MenuComboId = combo2.Id, DishId = dishes[1].Id, SortOrder = 10, Quantity = 1 },
            new() { MenuComboId = combo2.Id, DishId = dishes[3].Id, SortOrder = 20, Quantity = 1 }
        };
        db.MenuComboDishes.AddRange(links);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("DbSeeder: inserted 2 demo combos with {Count} combo-dish links.", links.Count);
    }
}
