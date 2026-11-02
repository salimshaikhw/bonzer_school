# syntax=docker/dockerfile:1
# Bonzer School ERP API.
#   docker build -t bonzer-school-api .
#   docker build -t bonzer-school-api --build-arg VERSION=1.4.0 .
# Normally run through docker-compose.yml (API + PostgreSQL). See docs/docker.md.

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, from the project files only, so the package layer is cached until a .csproj changes.
COPY Domain/Domain.csproj Domain/
COPY Application/Application.csproj Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY API/API.csproj API/
RUN dotnet restore API/API.csproj

COPY . .
ARG VERSION=1.0.0
RUN dotnet publish API/API.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false \
        /p:InformationalVersion=${VERSION}

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# fontconfig + fonts: QuestPDF (receipts, certificates) and NPOI (Excel column sizing) need system fonts on Linux.
# curl: container health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl fontconfig fonts-dejavu-core libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Writable folders for the non-root user: JSON log files and data-protection keys (mount volumes here).
RUN mkdir -p /app/Logs /app/keys && chown -R "$APP_UID" /app/Logs /app/keys

COPY --from=build /app/publish .

# Non-root user provided by the .NET 8 images.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeysPath=/app/keys
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "API.dll"]
