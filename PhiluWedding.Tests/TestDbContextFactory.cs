using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PhiluWedding.Data;

namespace PhiluWedding.Tests;

/// <summary>
/// Creates a throw-away <see cref="PhiluWeddingDbContext"/> backed by the
/// EF Core in-memory provider.
///
/// <para>
/// The in-memory provider is used because the tests exercise application
/// logic (seeding, ordering, idempotency) and not PostgreSQL specific
/// behaviour. Every test gets its own database name so the tests stay
/// independent of each other and can run in parallel.
/// </para>
/// </summary>
public static class TestDbContextFactory
{
    public static PhiluWeddingDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<PhiluWeddingDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new PhiluWeddingDbContext(options);
    }

    /// <summary>A logger that discards everything, used by the seeders.</summary>
    public static ILogger Logger => NullLogger.Instance;
}
