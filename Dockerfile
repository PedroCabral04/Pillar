# Build and run Pillar ERP (.NET 9 Blazor Server)

# ---------- Restore stage (best cache hit rate) ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

# Keep restore in its own layer so source-only changes do not invalidate NuGet cache
COPY erp.csproj ./
RUN dotnet restore "erp.csproj"

# ---------- Publish stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
WORKDIR /src

# Reuse restored packages and assets from previous stage
COPY --from=restore /root/.nuget /root/.nuget
COPY --from=restore /src .

# Copy source after restore to avoid re-restoring on each code change
COPY . .

RUN dotnet publish "erp.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Build-time arguments (optional)
ARG ASPNETCORE_ENVIRONMENT=Production
ARG DB_BOOTSTRAP=false

# Environment variables (overridable by Coolify at runtime)
ENV TZ=America/Sao_Paulo \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT} \
    DB_BOOTSTRAP=${DB_BOOTSTRAP} \
    DATAPROTECTION__KEYS_DIRECTORY=/keys

EXPOSE 8080

# Copy published output
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "erp.dll"]
