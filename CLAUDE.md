# Backend

## Buyruqlar
- Build:     dotnet build
- Test:      dotnet test  
- Run:       dotnet run --project src/Api
- Migration: dotnet ef migrations add --project src\VendlyServer.Infrastructure\VendlyServer.Infrastructure.csproj --startup-project src\VendlyServer.Api\VendlyServer.Api.csproj --context VendlyServer.Infrastructure.Persistence.AppDbContext --configuration Debug --verbose [Name]Migration --output-dir Persistence/Migrations