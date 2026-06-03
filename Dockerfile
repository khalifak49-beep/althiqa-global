# =====================================================================
#  Al Thiqa Global - .NET 9 Dockerfile (Render-compatible, simplified)
# =====================================================================

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV NUGET_XMLDOC_MODE=skip

COPY HomeMaids.csproj ./
RUN dotnet restore HomeMaids.csproj

COPY . ./
RUN dotnet publish HomeMaids.csproj \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false \
        /p:GenerateDocumentationFile=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "HomeMaids.dll"]
