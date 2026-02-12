# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore (layer caching)
COPY HammamAPI.Domain/HammamAPI.Domain.csproj HammamAPI.Domain/
COPY HammamAPI.Application/HammamAPI.Application.csproj HammamAPI.Application/
COPY HammamAPI.Infrastructure/HammamAPI.Infrastructure.csproj HammamAPI.Infrastructure/
COPY HammamAPI.WebAPI/HammamAPI.WebAPI.csproj HammamAPI.WebAPI/

RUN dotnet restore HammamAPI.WebAPI/HammamAPI.WebAPI.csproj

# Copy all source code
COPY . .

# Publish
RUN dotnet publish HammamAPI.WebAPI/HammamAPI.WebAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ============================================
# Stage 2: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser

# Create logs and uploads directories
RUN mkdir -p /app/logs /app/uploads && chown -R appuser:appuser /app/logs /app/uploads

COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

USER appuser

ENTRYPOINT ["dotnet", "HammamAPI.WebAPI.dll"]
