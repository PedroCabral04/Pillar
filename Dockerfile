# Build and run Pillar ERP (.NET 9 Blazor Server) in a container
# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY erp.csproj ./
COPY Pillar.ServiceDefaults/Pillar.ServiceDefaults.csproj Pillar.ServiceDefaults/
RUN dotnet restore "erp.csproj"

# copy everything else and build
COPY . .
RUN dotnet publish "erp.csproj" -c Release -o /app/publish --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Optional: set timezone to UTC explicitly (app already uses UTC)
ENV TZ=Etc/UTC \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Copy published output
COPY --from=build /app/publish .

# Default health probe (Coolify can use /health)
# HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "erp.dll"]
