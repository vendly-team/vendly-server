FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VendlyServer.slnx ./

COPY src/VendlyServer.Api/VendlyServer.Api.csproj src/VendlyServer.Api/
COPY src/VendlyServer.Application/VendlyServer.Application.csproj src/VendlyServer.Application/
COPY src/VendlyServer.Domain/VendlyServer.Domain.csproj src/VendlyServer.Domain/
COPY src/VendlyServer.Infrastructure/VendlyServer.Infrastructure.csproj src/VendlyServer.Infrastructure/

RUN dotnet restore src/VendlyServer.Api/VendlyServer.Api.csproj

COPY src/ src/

WORKDIR /src/src/VendlyServer.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libpng16-16 \
    libgcc-s1 \
    libstdc++6 \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "VendlyServer.Api.dll"]