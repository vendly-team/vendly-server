# Backend

## Buyruqlar
- Build:     dotnet build
- Test:      dotnet test  
- Run:       dotnet run --project src/Api
- Migration: dotnet ef migrations add --project src\VendlyServer.Infrastructure\VendlyServer.Infrastructure.csproj --startup-project src\VendlyServer.Api\VendlyServer.Api.csproj --context VendlyServer.Infrastructure.Persistence.AppDbContext --configuration Debug --verbose [Name]Migration --output-dir Persistence/Migrations

---

## File/Image Upload Rule

Any endpoint that accepts an image or file MUST follow this pattern:

### Request DTO
Use `IFormFile? Image` (not `string? ImageUrl`) in the request record.

### Controller
- Use `[FromForm]` binding — no upload logic here
- Pass the request DTO directly to the service

### Service
- Inject `IStorageService` alongside `AppDbContext`
- Call `storageService.UploadAsync(request.Image, "folder-name")` to get the URL
- Store the returned URL string in the entity
- On Update: upload new file first, then delete old URL via `storageService.DeleteAsync(oldUrl)`
- On Delete: soft-delete the entity, then call `storageService.DeleteAsync(imageUrl)`

### Interface (Infrastructure layer)
`IStorageService` lives in `VendlyServer.Infrastructure.Storage`.
Current implementation: `LocalStorageService` (stub — returns success, no real storage yet).

### NEVER
- Accept raw URL strings from clients for image fields
- Put upload/delete logic in the controller
- Store `IFormFile` or file bytes in the database