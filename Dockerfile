# =====================================================================
# Dockerfile — PhiLuWedding (ASP.NET Core 8 Razor Pages)
# Multi-stage build optimized for Railway / any container host that
# speaks the OCI image spec.
#
# Stage 1: build the published artifact using the SDK image.
# Stage 2: copy the published output into a slim runtime-only image.
#
# Build:  docker build -t philu-wedding .
# Run:    docker run -p 8080:8080 \
#             -e ASPNETCORE_ENVIRONMENT=Production \
#             -e ConnectionStrings__DefaultConnection=... \
#             philu-wedding
#
# The container reads its listen port from the $PORT environment variable
# (set by Railway) via Program.cs. ASPNETCORE_URLS is intentionally NOT
# baked in — the application code controls the bind so the same image
# works in every environment.
# =====================================================================

# ---- Stage 1: build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore as a separate layer so dependency restores are cached when
# only application code changes.
COPY PhiluWedding.csproj ./
RUN dotnet restore PhiluWedding.csproj

# Copy the rest of the source and publish a release build.
COPY . ./
RUN dotnet publish PhiluWedding.csproj \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

# ---- Stage 2: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# The published output from the build stage.
COPY --from=build /app/publish ./

# Run as a non-root user for safer container security defaults.
RUN groupadd --system --gid 1001 app \
    && useradd --system --uid 1001 --gid app app \
    && chown -R app:app /app
USER app

# Default ASP.NET Core port. The actual port Railway uses is supplied
# via the PORT env var, which Program.cs reads at startup.
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "PhiluWedding.dll"]