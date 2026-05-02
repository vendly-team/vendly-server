# Backend Rules
# Applies to: backend/**

## Arxitektura

```
Domain → Infrastructure → Application → API
```

```
Solution/
├── src/
│   ├── [Project].Domain          ← Biznes tushunchalar, entitylar, abstractsiyalar
│   ├── [Project].Infrastructure  ← EF Core, Brokerlar, Hub, Auth, Seed, 3rd-party
│   ├── [Project].Application     ← Biznes logika, servicelar, joblar, DTOlar
│   └── [Project].Api             ← HTTP endpointlar, controllerlar, middleware
```

| Layer | Bog'liqlik | Nima turadi |
|-------|------------|-------------|
| **Domain** | Hech narsaga bog'liq emas | Entity, Enum, Utils, Result pattern, Common errors |
| **Infrastructure** | Domain'ga bog'liq | DbContext, Migration, Broker, Hub, Auth config, Seed data, 3rd-party integratsiyalar |
| **Application** | Domain + Infrastructure'ga bog'liq | Service (interface + impl), Background job, DTO, Validator, Errors |
| **API** | Barcha layerlarga bog'liq | Controller, Middleware, Global Exception Handler, Filter |

Har layer faqat o'z vazifasini bajaradi — aralashtirilmaydi.

---

## Layer tarkibi

### Domain
```
Domain/
├── Abstractions/
│   ├── Result.cs / Result<T>.cs
│   ├── Error.cs / ErrorType.cs
│   └── DataQueryRequest.cs
├── Entities/
│   ├── Common/
│   │   ├── ModelBase.cs
│   │   └── AuditableModelBase.cs
│   └── [Entity].cs
├── Enums/
│   └── [EnumName].cs
└── Utils/
    └── [UtilName].cs
```

### Infrastructure
```
Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   └── Migrations/
├── Brokers/
│   └── [System]/
│       ├── I[System]Broker.cs
│       └── [System]Broker.cs
├── Hubs/
│   └── [Name]Hub.cs
├── Authorization/
│   └── [Name]AuthConfig.cs
├── Extensions/
│   └── Seed/
│       └── [Name]Seeder.cs
└── Dependencies.cs
```

### Application
```
Application/
├── Services/
│   └── [Entity]/
│       ├── Contracts/
│       │   ├── [Action][Entity]Request.cs
│       │   ├── [Entity]Response.cs
│       │   ├── [Entity]FilterRequest.cs
│       │   └── [Action][Entity]RequestValidator.cs
│       ├── [Entity]Errors.cs
│       ├── I[Entity]Service.cs
│       └── [Entity]Service.cs
├── Jobs/
│   └── [Category]/
│       ├── I[Name]Job.cs
│       └── [Name]Job.cs
└── Dependencies.cs
```

### API
```
Api/
├── Controllers/
│   ├── Common/
│   │   └── AuthorizedController.cs
│   └── [Scope]/
│       └── [Entity]sController.cs
├── Middlewares/
│   └── GlobalExceptionHandlerMiddleware.cs
├── Filters/
│   └── [Name]Filter.cs
└── Extensions/
    └── ResultExtensions.cs
```

---

## Naming

| Tur | Pattern | Misol |
|-----|---------|-------|
| Entity | `[Name]` | `Document` |
| Service interface | `I[Name]Service` | `IDocumentService` |
| Service impl | `[Name]Service` | `DocumentService` |
| Errors | `[Name]Errors` | `DocumentErrors` |
| Controller | `[Names]Controller` | `DocumentsController` |
| Request DTO | `[Action][Name]Request` | `CreateDocumentRequest` |
| Response DTO | `[Name]Response` | `DocumentResponse` |
| Filter DTO | `[Name]FilterRequest` | `DocumentFilterRequest` |
| Validator | `[RequestClass]Validator` | `CreateDocumentRequestValidator` |
| Job interface | `I[Name]Job` | `IBankSyncJob` |
| Job impl | `[Name]Job` | `BankSyncJob` |
| Broker interface | `I[System]Broker` | `IBankBroker` |
| Broker impl | `[System]Broker` | `BankBroker` |
| Enum | `[Name]` (singular) | `DocumentStatus` |
| Middleware | `[Name]Middleware` | `GlobalExceptionHandlerMiddleware` |
| Filter | `[Name]Filter` | `ModelValidationFilter` |

---

## Method nomlari

| Maqsad | Method |
|--------|--------|
| Yaratish | `AddAsync` / `CreateAsync` |
| Bitta olish | `GetByIdAsync` |
| Ro'yxat | `GetAllAsync` |
| Yangilash | `UpdateAsync` |
| O'chirish | `DeleteAsync` |
| Job execution | `ExecuteAsync` |

---

## Result Pattern

```csharp
// ✅ TO'G'RI — Result qaytarish
public async Task<Result<DocumentResponse>> GetByIdAsync(long id, ...)
{
    var doc = await dbContext.Documents
        .AsNoTracking()
        .Where(d => d.Id == id && !d.IsDeleted)
        .SingleOrDefaultAsync(cancellationToken);

    return doc is null ? DocumentErrors.NotFound : doc;
}

// ❌ NOTO'G'RI — exception throw
if (doc == null) throw new Exception("Not found");
```

---

## Entity

```csharp
// ✅ TO'G'RI
public class Document : AuditableModelBase<long>
{
    public required string Title { get; set; }
    public Contract Contract { get; set; } = null!;       // null! bilan
    public List<Agreement> Agreements { get; set; } = new(); // new() bilan
}
```

---

## Service

```csharp
// READ — AsNoTracking majburiy
.AsNoTracking().Where(d => !d.IsDeleted)...

// DELETE — IsDeleted = true, Remove() EMAS
doc.IsDeleted = true;
await dbContext.SaveChangesAsync(cancellationToken); // faqat Service da

// NULL check
var doc = await ...SingleOrDefaultAsync(...);
if (doc is null) return DocumentErrors.NotFound;
```

---

## Controller

```csharp
// XML comment — faqat Controller metodlarida
/// <summary>Get document by id.</summary>
[HttpGet("{id:long}")]
public async Task<IResult> GetByIdAsync(long id, ...)
{
    var result = await documentService.GetByIdAsync(id, cancellationToken);
    return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
}
```

---

## ⚠️ Qat'iy taqiqlar

- `throw` — ISHLATMA, `Result.Failure(error)` qaytар
- `Remove()` — ISHLATMA, `IsDeleted = true` ishlat
- `.First()` — ISHLATMA, `.SingleOrDefaultAsync()` ishlat
- `SaveChangesAsync` — Repository yoki Controller da EMAS, faqat Service da
- `AsNoTracking()` — read so'rovlarda MAJBURIY
- `.Where(x => !x.IsDeleted)` — har read so'rovda MAJBURIY
- Navigation property — `= null!` yoki `= new()` bilan initialize qil
- `int` Id — ISHLATMA, `long` ishlat
- XML comment — Service da YOZILMAYDI, faqat Controller da
- Biznes logika Controller'da — YOZILMAYDI, Service'ga ko'chir
- `dbContext` Controller'da inject — QILINMAYDI, faqat Service'da