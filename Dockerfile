# =====================================================================
#  Al Thiqa Global - .NET 9 Dockerfile (optimized for Render free tier)
#  - Multi-stage build with layer caching
#  - Disables memory-hungry MSBuild tasks (static asset compression)
#  - Honors PORT env var set by Render (handled in Program.cs)
# =====================================================================

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
WORKDIR /src

# Less verbose, no XML docs cache, conserve memory
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_GCConserveMemory=9
ENV DOTNET_gcServer=0

# Restore first (good layer caching)
COPY HomeMaids.csproj ./
RUN dotnet restore HomeMaids.csproj \
        -p:NoStaticWebAssetsCompression=true

# Build + publish
COPY . ./
RUN dotnet publish HomeMaids.csproj \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false \
        /p:GenerateDocumentationFile=false \
        /p:CompressionEnabled=false \
        /p:StaticWebAssetsBaseTaskOutputPath= \
        /p:CompressedFilesOutputPath= \
        /p:NoStaticWebAssetsCompression=true \
        /p:_StaticWebAssetsFingerprintContent=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_GCConserveMemory=9
ENV DOTNET_gcServer=0

EXPOSE 8080
ENTRYPOINT ["dotnet", "HomeMaids.dll"]
