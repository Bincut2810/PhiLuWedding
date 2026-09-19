using Microsoft.EntityFrameworkCore;
using PhiluWedding.Configuration;
using PhiluWedding.Data;
using PhiluWedding.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Cloudinary options + service. Configuration is loaded from the
// "Cloudinary" section and can be overridden by Cloudinary__CloudName,
// Cloudinary__ApiKey, Cloudinary__ApiSecret, Cloudinary__Folder.
// When credentials are missing the service is registered but degrades
// to a no-op so the homepage still renders without Cloudinary access.
builder.Services.Configure<CloudinaryOptions>(
    builder.Configuration.GetSection(CloudinaryOptions.SectionName));
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();
builder.Services.AddSingleton<IHallImageUrlResolver, HallImageUrlResolver>();
builder.Services.AddSingleton<IDishImageUrlResolver, DishImageUrlResolver>();

// PostgreSQL via Npgsql. The connection string is read from
// appsettings.json (ConnectionStrings:DefaultConnection) or
// environment variable ConnectionStrings__DefaultConnection.
// If the connection string is missing we still let the app start so the
// public homepage (which does not use the DB yet) continues to work.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var hasConnectionString = !string.IsNullOrWhiteSpace(connectionString);

if (hasConnectionString)
{
    builder.Services.AddDbContext<PhiluWeddingDbContext>(options =>
        options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(PhiluWeddingDbContext).Assembly.GetName().Name)));
}

// ===========================================================================
// Hosting — Railway / production
// ===========================================================================
// Railway assigns a dynamic PORT at runtime. ASP.NET Core's default
// Kestrel binding is wired through `applicationUrl` in launchSettings.json
// (used by `dotnet run` with a launch profile) and through the
// `ASPNETCORE_URLS` / `--urls` configuration keys. We translate
// Railway's PORT into a Kestrel binding ONLY when no other URL source
// is configured, so:
//   * `dotnet run` continues to honour launchSettings.json (http://localhost:5124).
//   * Setting ASPNETCORE_URLS manually still wins.
//   * On Railway, PORT is honoured because no other source sets a URL.
var hasExplicitUrls =
    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    || args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase));

if (!hasExplicitUrls)
{
    var port = Environment.GetEnvironmentVariable("PORT");
    var bindPort = !string.IsNullOrWhiteSpace(port) && int.TryParse(port, out var parsed)
        ? parsed
        : 8080;
    builder.WebHost.UseUrls($"http://0.0.0.0:{bindPort}");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Friendly 500 / exception page in production. Detailed errors are
    // only emitted by ASP.NET Core when the environment is Development;
    // we still log the full exception server-side.
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this
    // for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Developer-friendly exceptions in dev — let the browser show the
    // full exception detail (still never logged to stdout verbatim
    // when secrets could be present).
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// ===========================================================================
// Database initialization
// ===========================================================================
//
// EF Core migrations are the SINGLE SOURCE OF TRUTH for the schema.
// The application does NOT automatically apply migrations or seed data
// on startup. Both actions require explicit opt-in.
//
// Recommended (manual) workflow:
//   1. dotnet ef database update          -- apply migrations
//   2. dotnet run -- --seed               -- (development only) insert demo rows
//
// Opt-in startup behavior, OFF by default:
//   Database:AutoMigrateOnStartup = true   (config) or
//   Database__AutoMigrateOnStartup = true  (environment variable)
//
//   dotnet run -- --seed                   (CLI argument, runs the demo seeder)
//
// All startup database operations are BEST-EFFORT: failures are logged as
// warnings and the application keeps serving. NEVER run destructive database
// operations automatically (no EnsureDeleted, no DROP DATABASE, etc.).
//
// Secret safety:
//   * We NEVER log the full connection string.
//   * We NEVER log Cloudinary ApiSecret / signed upload payloads.
//   * Errors from Cloudinary are returned by the SDK and are safe to log.
//   * The seeder only inserts rows prefixed with "DEMO" / "SẢNH DEMO" so
//     they can never be confused with real Phì Lũ business data.
// ===========================================================================
var autoMigrate = builder.Configuration.GetValue<bool>("Database:AutoMigrateOnStartup");
var seedRequested = args.Any(a => string.Equals(a, "--seed", StringComparison.OrdinalIgnoreCase));

if (hasConnectionString && (autoMigrate || seedRequested))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");
    var db = scope.ServiceProvider.GetService<PhiluWeddingDbContext>();
    if (db is not null)
    {
        try
        {
            if (autoMigrate)
            {
                logger.LogInformation("Applying pending migrations (Database:AutoMigrateOnStartup=true)...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied.");
            }

            if (seedRequested)
            {
                // When --seed is requested the caller almost certainly wants
                // demo content right now. Ensure the schema is in place before
                // seeding so a fresh checkout works with one command.
                if (!autoMigrate)
                {
                    logger.LogInformation("Applying pending migrations before seeding (--seed requested)...");
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied.");
                }
                await DbSeeder.SeedAsync(db, logger);
            }
        }
        catch (Exception ex)
        {
            // Never log the raw exception object together with the
            // connection string. We log the message only and let the
            // hosting platform capture the full stack via its own sink.
            logger.LogWarning("Database operation skipped: {Message}", ex.Message);
        }
    }
}

app.Run();