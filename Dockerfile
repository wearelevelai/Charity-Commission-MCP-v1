# syntax=docker/dockerfile:1.7

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and restore as distinct layers
COPY CCEW.Mcp.sln ./
COPY Directory.Build.props ./
COPY src/CCEW.Mcp.Server/CCEW.Mcp.Server.csproj src/CCEW.Mcp.Server/
COPY tests/CCEW.Mcp.ContractTests/CCEW.Mcp.ContractTests.csproj tests/CCEW.Mcp.ContractTests/
RUN dotnet restore CCEW.Mcp.sln

# Copy the rest of the source
COPY . .

# Build and publish
RUN dotnet publish src/CCEW.Mcp.Server/CCEW.Mcp.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --home /nonroot --gecos "" appuser \
    && chown -R appuser:appuser /app
USER appuser

# Copy published output
COPY --from=build /app/publish .

# Expose HTTP port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Optional basic healthcheck that relies on /healthz endpoint
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/healthz | grep -q '"status":"ok"' || exit 1

ENTRYPOINT ["dotnet", "CCEW.Mcp.Server.dll"]
