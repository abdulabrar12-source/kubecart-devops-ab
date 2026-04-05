# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies (cached layer unless .csproj changes)
COPY IdentityService.csproj ./
RUN dotnet restore IdentityService.csproj

# Copy source and publish
COPY . .
RUN dotnet publish IdentityService.csproj \
    --configuration Release \
    --self-contained false \
    --output /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as non-root for security
RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

# Copy published output from build stage
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80

ENTRYPOINT ["dotnet", "IdentityService.dll"]
