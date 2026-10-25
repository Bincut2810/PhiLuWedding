# Nhà Hàng Tiệc Cưới Phì Lũ — Website

Premium wedding venue website for **Nhà Hàng Phì Lũ**, Đà Nẵng, Vietnam.

> **Phase 08 — Conversion UX Polish + Production Readiness**
> The current build is ready for Railway deployment. The booking flow
> supports hall preselection, the success state is server-side protected,
> the schema has a small nullable FK for the preferred hall, and the
> application respects Railway's `PORT` environment variable.

---

## Tech stack

- **.NET** 8.0 (ASP.NET Core Razor Pages)
- **Razor Pages** with server-rendered HTML
- **PostgreSQL** via Npgsql + Entity Framework Core 8
- **Cloudinary** — credentials-driven media service (no-op when unconfigured)
- **Railway** — production hosting (Dockerfile included, buildpacks also work)
- **Custom CSS** only — no UI framework, no Bootstrap, no Tailwind
- **Vanilla JavaScript** — for the mobile nav toggle only

Vietnamese-first website (`lang="vi"`), Google Fonts (Cormorant Garamond + Inter).

---

## Project structure

```
PhiluWedding/
├── Pages/
│   ├── Index.cshtml            # Homepage
│   ├── Index.cshtml.cs
│   ├── DatTiec.cshtml          # Booking consultation request form
│   ├── DatTiec.cshtml.cs
│   ├── DatTiec/
│   │   └── BookingFormInput.cs # View-model with DataAnnotations
│   ├── TiecCuoi.cshtml         # Hall listing
│   ├── TiecCuoi.cshtml.cs
│   ├── TiecCuoi/
│   │   ├── Details.cshtml      # Hall detail
│   │   └── Details.cshtml.cs
│   ├── Error.cshtml            # Production-friendly error page
│   ├── Error.cshtml.cs
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   └── Shared/
│       ├── _Layout.cshtml      # Master layout
│       ├── _Header.cshtml      # Header partial
│       ├── _Footer.cshtml      # Footer partial
│       └── _ValidationScriptsPartial.cshtml
├── wwwroot/
│   ├── css/
│   │   ├── site.css            # Design tokens, header, footer, typography
│   │   ├── home.css            # Homepage section styles
│   │   ├── halls.css           # Hall listing + detail styles
│   │   └── booking.css         # Booking page + trust panel + success state
│   ├── js/
│   │   └── site.js             # Mobile nav toggle, accessibility helpers
│   └── images/                 # (Cloudinary-backed in production)
├── Configuration/
│   └── CloudinaryOptions.cs    # Strongly-typed Cloudinary settings
├── Data/
│   ├── PhiluWeddingDbContext.cs
│   └── Configurations/         # EF Core Fluent API per entity
├── Domain/
│   ├── Entities/               # WeddingHall, HallImage, MenuSet, MenuTier, Dish, BookingRequest, ...
│   ├── Enums/BookingStatus.cs
│   ├── MenuPriceTypes.cs       # How a price is quoted (Fixed, From, Range, ...)
│   └── MenuVenueScopes.cs      # Which venue a menu row applies to
├── Migrations/                 # EF Core migrations (single source of truth)
├── Seed/
│   └── menu-catalog.json       # Fixed menu catalog (version controlled, embedded)
├── Services/
│   ├── CloudinaryService.cs
│   ├── DbSeeder.cs             # Opt-in demo seeder (DEMO-prefixed rows)
│   ├── MenuCatalogSeeder.cs    # Idempotent menu catalog seeder
│   ├── MenuCatalogDocument.cs  # Typed view of Seed/menu-catalog.json
│   ├── MenuSeedReport.cs       # What the seeder created / kept / conflicted on
│   ├── HallImageUrlResolver.cs
│   └── ICloudinaryService.cs
├── PhiluWedding.Tests/         # xUnit test project
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── Program.cs                  # Hosting, port binding, pipeline, DB init
├── PhiluWedding.csproj
├── Dockerfile                  # Multi-stage build for Railway / containers
├── .dockerignore
├── .gitignore
└── README.md
```

---

## Running locally

```powershell
cd d:\PhiLuWedding
dotnet restore
dotnet build
dotnet run
```

The site will be available at the URL printed in the terminal
(default `http://localhost:5124` — see `Properties/launchSettings.json`).

If `ConnectionStrings__DefaultConnection` is set in the environment
(or present in `appsettings.json`), the booking form will persist to
PostgreSQL. Otherwise the public site still renders and the booking
page shows a friendly "Hệ thống đang bận" notice.

---

## Deployment to Railway

The application reads its bind port from the `PORT` environment variable
that Railway injects at runtime. No code or appsettings changes are
required for Railway to host the app.

### One-time setup

1. Push the repository to a Git provider that Railway can access
   (GitHub, GitLab, etc.). The project now ships with a `.gitignore`,
   `Dockerfile`, and `.dockerignore`.
2. In Railway, **New Project → Deploy from GitHub repo**, select the
   repository. Railway auto-detects the `Dockerfile`.
3. **Add a PostgreSQL plugin** in the same Railway project. Railway
   prints a `DATABASE_URL` for the plugin in the format
   `postgresql://user:password@host:port/database?sslmode=require`.
   Convert it into an Npgsql connection string (key/value form, e.g.
   `Host=...;Port=...;Database=...;Username=...;Password=...;SslMode=Require`)
   and assign it to `ConnectionStrings__DefaultConnection`. The
   application does **not** read `DATABASE_URL` directly; it reads
   `ConnectionStrings__DefaultConnection` via the standard ASP.NET Core
   configuration system.

### Required environment variables

Set these in the Railway service (no real values committed to the repo):

| Variable                              | Purpose                                              |
| ------------------------------------- | ---------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`              | Set to `Production` for Railway.                     |
| `PORT`                                | Set automatically by Railway. The app reads it.      |
| `ConnectionStrings__DefaultConnection`| Npgsql connection string from the PostgreSQL plugin. |
| `Cloudinary__CloudName`               | Cloudinary cloud name.                               |
| `Cloudinary__ApiKey`                  | Cloudinary API key.                                  |
| `Cloudinary__ApiSecret`               | Cloudinary API secret.                               |
| `Cloudinary__Folder`                  | Cloudinary folder (default `philu-wedding`).         |

### Database migration

Production **must NOT** auto-apply migrations on container start.
Apply the migration once from a developer workstation or a one-off
Railway shell:

```powershell
# Set the SAME Npgsql connection string the Railway service uses and
# then apply migrations against the production database:
$env:ConnectionStrings__DefaultConnection = "Host=...;Port=...;Database=...;Username=...;Password=...;SslMode=Require"
dotnet ef database update
```

The application reads the connection string from
`ConnectionStrings__DefaultConnection` (env var form) or
`ConnectionStrings:DefaultConnection` (appsettings form). The plain
`DATABASE_URL` URL is **not** consumed by the application — it must be
converted to the Npgsql key/value form first.

Do **not** set `Database:AutoMigrateOnStartup` in production — the
opt-in flag exists for development convenience only.

### Seed data

There are two independent seeders.

**Demo seed (development only).** The `DbSeeder` only ever inserts rows
prefixed with `DEMO` / `SẢNH DEMO` / `MENU DEMO` / `COMBO DEMO` / `MÓN DEMO`.
To seed a fresh database for development:

```powershell
dotnet run -- --seed
```

Never run `--seed` in production.

**Menu catalog seed.** The fixed wedding-menu catalog (eight shared price
tiers, seventeen menu options, the ordered dish lists, and the additional
menu sections) lives in the version-controlled file `Seed/menu-catalog.json`
and is embedded into the assembly. To apply it:

```powershell
dotnet run -- --seed-menu
```

The seeder is **idempotent**:

- Nothing is ever deleted.
- Rows that already exist are left untouched; only missing rows are inserted.
- Prices are written when a price row is first created. A price an
  administrator has edited is never reset — the difference is logged as a
  conflict instead.
- Publication flags and admin-maintained notes are never reset.
- Every record is matched on its own stable key, so one existing row never
  blocks the rest of the catalog from being seeded.

You can also enable it with `Database:SeedMenuOnStartup = true`
(or `Database__SeedMenuOnStartup=true`), but the CLI flag is preferred
because startup seeding runs on every boot.

#### Venue applicability
Menu content carries an explicit `venue_scope`:

| Scope | Meaning |
|---|---|
| `Both` | Confirmed for Phì Lũ **and** Golden Phoenix |
| `PhiLu` | Confirmed for Phì Lũ only |
| `GoldenPhoenix` | Confirmed for Golden Phoenix only |

The price tiers and the wedding menu set are `Both`. Everything that comes
from the Phì Lũ source price sheet — services, beverages, appetizer nuts,
promotions and deposit terms — is `PhiLu` until the business confirms it
also applies to Golden Phoenix. `Both` always means "confirmed for both",
never "unknown".

### Public wedding-menu pages

Two public pages show the wedding-menu price sheet:

| URL | Venue |
|---|---|
| `/thuc-don/philu` | Trung tâm Hội nghị Tiệc cưới Phì Lũ |
| `/thuc-don/golden-phoenix` | Golden Phoenix |

Both pages share one body (`Pages/Shared/_MenuVenueContent.cshtml`) and one
page model (`Pages/ThucDon/MenuVenuePageModel.cs`), so the eight price tiers
and their options can never drift apart between the two venues. The pages
are read-only: they never write, migrate or seed.

The price sheet shows, in this order: the eight price tiers (one tile per
tier, the selected tier is opened with `?muc=<slug>`), the menu options of
the selected tier, the additional services, the beverages, the appetizer
nuts, the promotion tiers with their conditions, the deposit terms and the
notes about the prices.

Every wording comes from the source price sheet:

- The unit of a price is shown only where the source states one.
- The two beverage prices keep the "dưới 25 bàn" / "trên 25 bàn" wording,
  including the note that the source says nothing about exactly 25 tables.
- A row without a numeric price shows the source wording instead of `0đ`.
- The promotion tiers list the benefits of the tier they inherit from, and
  the conditions apply to the tiers as a whole.
- The price notes keep the validity date and the VAT note of the source.

Sections that are scoped to one venue are only shown on that venue's page.
Golden Phoenix therefore shows a short notice that the services, beverages,
appetizer nuts, promotions and deposit terms are currently published for
Phì Lũ.

### Public URL

After the first successful deploy, Railway assigns a public URL such as
`https://<service>.up.railway.app`. Do not hardcode this URL anywhere
in the application; the platform sets it automatically.

---

## Tests

```powershell
dotnet test PhiluWedding.Tests\PhiluWedding.Tests.csproj
```

The test project uses xUnit with the EF Core in-memory provider. It covers
the menu catalog data integrity (tier and option counts, option ordering,
repeated dishes across options, Vietnamese round-trip, service / beverage /
nut / promotion / deposit semantics), the shared Option C pricing, the
idempotency of the seeder and the independence of prices from dish
composition, and the two public menu pages (shared tiers and prices,
option names, the beverage units and threshold, the venue scope of the
supporting sections, the inherited promotion benefits and the deposit
terms).

The tests do not require a running database.

---

## Design system (Phase 01 baseline)

### Colors

| Token              | Value      | Use                                  |
| ------------------ | ---------- | ------------------------------------ |
| `--color-ivory`    | `#F8F6F0`  | Soft section backgrounds             |
| `--color-cream`    | `#F1EBDD`  | Editorial section background         |
| `--color-paper`    | `#FFFDF9`  | Default page background              |
| `--color-brown`    | `#4A3A2E`  | Primary text, primary button         |
| `--color-ink`      | `#2C241C`  | Headings, hover states               |
| `--color-muted-brown` | `#8A7866` | Eyebrow, metadata, supporting text |
| `--color-champagne` | `#CDBA9B` | Accents (used sparingly)            |
| `--color-border`   | `#D9CDBB`  | Thin decorative borders              |
| `--color-line`     | `#E5DCC9`  | Inner offset border                  |

### Typography

- **Serif** — Cormorant Garamond (logo mark, headings, hall names)
- **Sans-serif** — Inter (navigation, body, buttons, eyebrow, metadata)

### Breakpoints

- `≥ 600px` — Halls grid: 2 columns
- `≥ 768px` — Mobile nav becomes horizontal; footer multi-column
- `≥ 1024px` — Hero split layout; halls grid: 4 columns
- `≥ 1280px` — Container max width

Spacing scale: 4 / 8 / 12 / 16 / 24 / 32 / 40 / 48 / 64 / 80 / 96 / 128 px.

---

## Temporary content

All copy in this phase is **placeholder text** for visual foundation:

- Hall names use `SẢNH 01` – `SẢNH 04`.
- Hall descriptions are temporary.
- Contact information in the footer is intentionally marked
  `(cập nhật sau)`.
- Hero and intro images come from Unsplash and are clearly marked as
  `hình minh họa tạm thời` in alt text. They will be replaced with real
  Cloudinary assets in a later phase.

---

## Scope restrictions (Phase 01)

The following are **not** part of this phase and must not be implemented yet:

- PostgreSQL / Entity Framework Core
- Database migrations
- Cloudinary (image storage / transformations)
- Admin CMS, authentication, role management
- Booking database and form submission
- CRUD for halls, menus, dishes
- Payment, email notification
- Railway deployment
- Tenten DNS configuration
- Real business content, awards, history, customer reviews

---

## Roadmap (next phases, not started)

1. Real Cloudinary integration for images and gallery.
2. PostgreSQL + EF Core for halls, menus, dishes, bookings.
3. Admin CMS with authentication.
4. Hall detail pages and gallery.
5. Menu combos and dish detail pages.
6. Booking consultation form with persistence.
7. Railway deployment + Tenten DNS.
8. SEO, Open Graph, sitemap, analytics.
