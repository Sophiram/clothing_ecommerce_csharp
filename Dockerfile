# =========================================================================
# Multi-Stage Dockerfile for WebApplication_ClothingEcommerce (ASP.NET Core 9.0)
# =========================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["WebApplication_ClothingEcommerce.csproj", "./"]
RUN dotnet restore "WebApplication_ClothingEcommerce.csproj"

# Copy source code and build project
COPY . .
RUN dotnet publish "WebApplication_ClothingEcommerce.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Expose ports (8080 default non-root port for .NET 8+)
EXPOSE 8080
EXPOSE 8081

# Copy published artifacts
COPY --from=build /app/publish .

# Environment Defaults
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Security: Run as non-root app user
USER app

ENTRYPOINT ["dotnet", "WebApplication_ClothingEcommerce.dll"]
