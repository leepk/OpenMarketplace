# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution và project trước để tận dụng cache
COPY OpenMarketplace.sln .
COPY src/OpenMarketplace.Api/OpenMarketplace.Api.csproj src/OpenMarketplace.Api/
COPY src/OpenMarketplace.Application/OpenMarketplace.Application.csproj src/OpenMarketplace.Application/
COPY src/OpenMarketplace.Domain/OpenMarketplace.Domain.csproj src/OpenMarketplace.Domain/
COPY src/OpenMarketplace.Infrastructure/OpenMarketplace.Infrastructure.csproj src/OpenMarketplace.Infrastructure/
COPY src/OpenMarketplace.Shared/OpenMarketplace.Shared.csproj src/OpenMarketplace.Shared/

RUN dotnet restore OpenMarketplace.sln

# Copy toàn bộ source
COPY . .

RUN dotnet publish src/OpenMarketplace.Api/OpenMarketplace.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5001

ENTRYPOINT ["dotnet", "OpenMarketplace.Api.dll"]
