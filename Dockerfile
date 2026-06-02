# =====================================================================
#  Al Thiqa Global - .NET 9 multi-stage Dockerfile
#  Built for Render / any container host.
# =====================================================================

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY HomeMaids.csproj ./
RUN dotnet restore HomeMaids.csproj

COPY . ./
RUN dotnet publish HomeMaids.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Render sets $PORT dynamically; default 8080 for local container runs
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "HomeMaids.dll"]
