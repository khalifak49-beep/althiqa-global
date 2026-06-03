# =====================================================================
#  Al Thiqa Global - .NET 9 (tuned for Render 512MB free tier)
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

# Aggressive GC tuning for 512MB containers
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=9
ENV DOTNET_GCHeapHardLimit=314572800
ENV DOTNET_GCDynamicAdaptationMode=1
ENV DOTNET_GCRetainVM=0
ENV DOTNET_TieredCompilation=1
ENV DOTNET_TC_QuickJit=1
ENV DOTNET_ReadyToRun=0
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV ASPNETCORE_DETAILEDERRORS=true

EXPOSE 8080
ENTRYPOINT ["dotnet", "HomeMaids.dll"]
