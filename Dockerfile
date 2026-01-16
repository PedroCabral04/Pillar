# Build and run Pillar ERP (.NET 9 Blazor Server) in a container
# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY erp.csproj ./
RUN dotnet restore "erp.csproj"


# copy everything else and build
COPY . .
RUN dotnet publish "erp.csproj" -c Release -o /app/publish --no-restore

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
COPY --from=build /app/publish .

# Health probe (Coolify uses this for container health monitoring)

ENTRYPOINT ["dotnet", "erp.dll"]
