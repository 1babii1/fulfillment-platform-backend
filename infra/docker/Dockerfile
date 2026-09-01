FROM mcr.microsoft.com/dotnet/sdk:10.0.100-noble AS build
WORKDIR /workspace

COPY Directory.Build.props FulfillmentPlatform.slnx ./
COPY src ./src

RUN dotnet restore src/Api/Fulfillment.Api/Fulfillment.Api.csproj
RUN dotnet publish src/Api/Fulfillment.Api/Fulfillment.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build --chown=app:app /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080
USER app

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Fulfillment.Api.dll"]
