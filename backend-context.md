# CONTEXT.md — .NET Clean Architecture Reference

> Developer uchun cheatsheet: yangi feature, bug fix va refactor uchun barcha asosiy pattern va konvensiyalar.

---

## 1. Architecture Overview

```
Solution/
├── src/
│   ├── [Project].Domain          ← Biznes tushunchalar, entitylar, abstractsiyalar
│   ├── [Project].Application     ← Biznes logika, servicelar, joblar, DTOlar
│   ├── [Project].Infrastructure  ← EF Core, JWT, Brokerlar, tashqi integratsiyalar
│   └── [Project].Api             ← HTTP endpointlar, controllerlar, middleware
```

| Layer | Javobgarlik | Bog'liqlik |
|-------|------------|------------|
| **Domain** | Entity, Value Object, Result/Error abstraktsiyalari | Hech narsaga bog'liq emas |
| **Application** | Service interface va implementatsiyalari, DTO, Validator, Job | Domain'ga bog'liq |
| **Infrastructure** | DbContext, Migration, Broker, JWT, SignalR | Application + Domain |
| **API** | Controller, Middleware, Filter, Swagger konfiguratsiyasi | Application |

**Texnologiya steki:**
- Runtime: **.NET 8+**
- ORM: **Entity Framework Core** + **Npgsql** (PostgreSQL)
- Naming: **snake_case** (`.UseSnakeCaseNamingConvention()`)
- Validation: **FluentValidation**
- Background jobs: **Hangfire** (PostgreSQL storage)
- Auth: **JWT Bearer**
- Real-time: **SignalR**
- API docs: **Swagger / OpenAPI**

---

## 2. Core Pattern Principles

### Result Pattern (Railway-Oriented Programming)
Business error uchun **hech qachon exception throw qilinmaydi**. Barcha metodlar `Result` yoki `Result<T>` qaytaradi.

```csharp
// Domain/Abstractions/Result.cs
public record Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static implicit operator Result(Error error) => Failure(error);
}

public record Result<T> : Result
{
    public T? Data { get; }

    public static Result<T> Success(T data) => new(true, Error.None, data);
    public static Result<T> Failure(Error error) => new(false, error, default);
    public static implicit operator Result<T>(T data)  => Success(data);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
```

```csharp
// Domain/Abstractions/Error.cs
public record Error(string Code, ErrorType Type, string? Message = null)
{
    public static readonly Error None = new("", ErrorType.Failure);
    public static readonly Error NullValue = new("Error.NullValue", ErrorType.Failure);

    public static Error NotFound(string code) => new(code, ErrorType.NotFound);
    public static Error Validation(string code, string message) => new(code, ErrorType.Validation, message);
    public static Error Conflict(string code) => new(code, ErrorType.Conflict);
    public static Error Failure(string code) => new(code, ErrorType.Failure);
}

public enum ErrorType { Failure = 0, Validation = 1, NotFound = 2, Conflict = 3 }
```

### Soft Delete
`DELETE FROM` **hech qachon** ishlatilmaydi. Barcha entitylarda `IsDeleted` (snake_case: `is_deleted`) maydoni bor.

```csharp
dbContext.Documents.Remove(doc);          // ❌ ISHLATMA
doc.IsDeleted = true;
await dbContext.SaveChangesAsync();       // ✅ TO'G'RI
```

### Audit Trail
`SaveChanges` override'da `CreatedAt` / `UpdatedAt` **avtomatik** set qilinadi — qo'lda belgilash shart emas.

### Service-Based Architecture (CQRS emas)
Barcha biznes logika `*Service` classlarda. Alohida Command/Query/Handler classlar yo'q.

### Modular DI Registration
Har layer `Dependencies.cs` static classida `Configure*()` extension metod pattern'dan foydalanadi.

```csharp
// Program.cs
builder.Services
    .ConfigureApplication()
    .ConfigureInfrastructure(builder.Configuration)
    .ConfigureSwagger()
    .ConfigureControllers()
    .ConfigureCors()
    .ConfigureHangfire(builder.Configuration);
```

---

## 3. Naming Conventions

### Classlar va Interfacelar

| Tur | Pattern | Misol |
|-----|---------|-------|
| Entity | `[Name]` | `Document`, `Agreement`, `Position` |
| Service interface | `I[Name]Service` | `IDocumentService` |
| Service impl | `[Name]Service` | `DocumentService` |
| Errors class | `[Name]Errors` | `DocumentErrors` |
| Controller | `[Names]Controller` (plural) | `DocumentsController` |
| Request DTO | `[Action][Name]Request` | `CreateDocumentRequest`, `PositionRequest` |
| Response DTO | `[Name]Response` | `DocumentResponse` |
| Filter DTO | `[Name]FilterRequest` | `DocumentFilterRequest` |
| Validator | `[RequestClass]Validator` | `CreateDocumentRequestValidator` |
| Background job iface | `I[Name]Job` | `IBankSyncJob`, `IScoringJob` |
| Background job impl | `[Name]Job` | `BankSyncJob`, `ScoringJob` |
| Job registrar | `JobsRegistrar` / `JobRegisterer` | `JobsRegistrar` |
| Broker interface | `I[System]Broker` | `IJiraBroker`, `IRedisCacheBroker` |
| Broker impl | `[System]Broker` | `JiraBroker`, `RedisCacheBroker` |
| Enum | `[Name]` (singular) | `DocumentStatus`, `Role` |
| Middleware | `[Name]Middleware` | `GlobalExceptionHandlerMiddleware` |
| Filter | `[Name]Filter` | `ModelValidationFilter`, `EnumSchemaFilter` |

### Method Naming (Service metodlari)

| Maqsad | Method nomi |
|--------|-------------|
| Yaratish | `AddAsync` yoki `CreateAsync` |
| Bitta olish | `GetByIdAsync` yoki `RetrieveByIdAsync` |
| Ro'yxat olish | `GetAllAsync` yoki `RetrieveAllAsync` |
| Yangilash | `UpdateAsync` |
| O'chirish | `DeleteAsync` |
| Tashqi import | `ImportAsync` |
| Yuborish | `SendAsync` |
| Job execution | `ExecuteAsync` / `Execute[Name]Async` |


### Database Naming
- **Jadval nomlari:** snake_case (EF Core `.UseSnakeCaseNamingConvention()` orqali avtomatik)
- **Ustun nomlari:** snake_case (avtomatik)
- **Primary key:** `id`
- **Audit ustunlari:** `created_at`, `updated_at`, `created_by`, `updated_by`
- **Soft delete:** `is_deleted`


## 4. Domain Layer

**Joylashuvi:** `[Project].Domain/`

### Fayl tuzilmasi

```
Domain/
├── Abstractions/
│   ├── Result.cs             ← Result, Result<T>
│   ├── Error.cs              ← Error record, ErrorType enum
│   └── DataQueryRequest.cs   ← PagedList<T>, SortDirection, filter base
├── Entities/
│   ├── Common/
│   │   ├── ModelBase.cs          ← abstract, Id + IsDeleted
│   │   └── AuditableModelBase.cs ← + CreatedAt, UpdatedAt (± CreatedBy, UpdatedBy)
│   └── [Entity].cs
├── Enums/
│   └── [EnumName].cs
└── Exceptions/
    ├── ApiException.cs
    └── NotFoundException.cs
```

### Base Classlar

```csharp
// Domain/Entities/Common/ModelBase.cs
public abstract class ModelBase<TId> where TId : struct
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public TId Id { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

// Domain/Entities/Common/AuditableModelBase.cs
public abstract class AuditableModelBase<TId> : ModelBase<TId> where TId : struct
{
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Ba'zi loyihalarda qo'shimcha:
    // [Column("created_by")] public long? CreatedBy { get; set; }
    // [Column("updated_by")] public long? UpdatedBy { get; set; }
}
```

### Entity misoli

```csharp
public class Document : AuditableModelBase<long>
{
    [MaxLength(255)]
    public required string Title { get; set; }

    public DocumentStatus Status { get; set; }

    [Column("contract_id")]
    public long ContractId { get; set; }

    [ForeignKey(nameof(ContractId))]
    public Contract Contract { get; set; } = null!;

    public List<Agreement> Agreements { get; set; } = new();
}
```

### Enum misoli

```csharp
public enum DocumentStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4
}
```

---

## 5. Application Layer

**Joylashuvi:** `[Project].Application/`

### Fayl tuzilmasi

```
Application/
├── Services/
│   └── [EntityName]/
│       ├── I[EntityName]Service.cs
│       ├── [EntityName]Service.cs
│       ├── [EntityName]Errors.cs
│       └── Contracts/
│           ├── [Entity]Request.cs
│           ├── [Entity]Response.cs
│           ├── [Entity]FilterRequest.cs    (kerak bo'lsa)
│           └── [Entity]RequestValidator.cs
├── Jobs/
│   ├── [Category]/
│   │   ├── I[Name]Job.cs
│   │   └── [Name]Job.cs
│   └── JobsRegistrar.cs
├── Dependencies.cs
└── GlobalUsings.cs
```

### Errors Class

```csharp
// Application/Services/Documents/DocumentErrors.cs
public static class DocumentErrors
{
    public static readonly Error NotFound = Error.NotFound("Document.NotFound");
    public static readonly Error AlreadyExists = Error.Conflict("Document.AlreadyExists");
    public static readonly Error InvalidStatus = Error.Failure("Document.InvalidStatus");
}
```

### DTO misollari

```csharp
// Contracts/CreateDocumentRequest.cs
public record CreateDocumentRequest
{
    public required string Title { get; set; }
    public long ContractId { get; set; }
}

// Contracts/DocumentResponse.cs
public record DocumentResponse
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Contracts/DocumentFilterRequest.cs  (DataQueryRequest'dan inherit)
public record DocumentFilterRequest : DataQueryRequest
{
    public DocumentStatus? Status { get; set; }
}
```

### Validator

```csharp
// Contracts/CreateDocumentRequestValidator.cs
public class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequest>
{
    public CreateDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters.");

        RuleFor(x => x.ContractId)
            .GreaterThan(0).WithMessage("ContractId must be greater than 0.");
    }
}
```

### Service Interface va Implementation

```csharp
// I[Entity]Service.cs
public interface IDocumentService
{
    Task<Result<List<DocumentResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<DocumentResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(long id, CreateDocumentRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);
}

// [Entity]Service.cs
public class DocumentService(AppDbContext dbContext) : IDocumentService
{
    public async Task<Result<List<DocumentResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .Select(d => new DocumentResponse { Id = d.Id, Title = d.Title, Status = d.Status })
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<DocumentResponse>> GetByIdAsync(
        long id, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.Id == id && !d.IsDeleted)
            .Select(d => new DocumentResponse { Id = d.Id, Title = d.Title, Status = d.Status })
            .SingleOrDefaultAsync(cancellationToken);

        return document is null ? DocumentErrors.NotFound : document;
    }

    public async Task<Result> AddAsync(
        CreateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var document = new Document { Title = request.Title, ContractId = request.ContractId };
        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(
        long id, CreateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents
            .SingleOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);

        if (document is null) return DocumentErrors.NotFound;

        document.Title = request.Title;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents
            .SingleOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);

        if (document is null) return DocumentErrors.NotFound;

        document.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

### Dependencies.cs

```csharp
// Application/Dependencies.cs
public static class Dependencies
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateDocumentRequestValidator>();

        services
            .AddScoped<IDocumentService, DocumentService>()
            .AddScoped<IAgreementService, AgreementService>();

        services
            .AddScoped<IBankSyncJob, BankSyncJob>()
            .AddScoped<IScoringJob, ScoringJob>();

        return services;
    }
}
```

### Background Jobs

```csharp
// Jobs/BankSync/IBankSyncJob.cs
public interface IBankSyncJob
{
    Task ExecuteLoanStatusJobAsync();
    Task ExecuteDailyLoanAccountsJobAsync();
}

// Jobs/BankSync/BankSyncJob.cs
public class BankSyncJob(AppDbContext dbContext, IBankBroker bankBroker) : IBankSyncJob
{
    public async Task ExecuteLoanStatusJobAsync()
    {
        // implementation
    }

    public async Task ExecuteDailyLoanAccountsJobAsync()
    {
        // implementation
    }
}

// Jobs/JobsRegistrar.cs
public static class JobsRegistrar
{
    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<IBankSyncJob>(
            "bank-sync-loan-status",
            job => job.ExecuteLoanStatusJobAsync(),
            "*/30 * * * *");

        RecurringJob.AddOrUpdate<IBankSyncJob>(
            "bank-sync-daily-loan-accounts",
            job => job.ExecuteDailyLoanAccountsJobAsync(),
            "0 1 * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
    }
}
```

---

## 6. Infrastructure Layer

**Joylashuvi:** `[Project].Infrastructure/`

### Fayl tuzilmasi

```
Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   └── Migrations/
│       └── [YYYYMMDDHHmm_Name].cs
├── Authentication/
│   ├── IJwtProvider.cs
│   ├── JwtProvider.cs
│   ├── JwtOptions.cs
│   ├── JwtOptionsSetup.cs
│   ├── JwtBearerOptionsSetup.cs
│   └── CustomClaims.cs
├── Brokers/
│   └── [SystemName]/
│       ├── I[SystemName]Broker.cs
│       ├── [SystemName]Broker.cs
│       └── Contracts/
│           ├── [Name]Request.cs
│           └── [Name]Response.cs
├── Extensions/
│   ├── ResultExtensions.cs
│   └── QueryableExtensions.cs
├── Hubs/
│   └── [Name]Hub.cs
├── Dependencies.cs
└── GlobalUsings.cs
```

### AppDbContext

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents { get; set; }
    public DbSet<Agreement> Agreements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite key misoli
        modelBuilder.Entity<DocumentAgreement>()
            .HasKey(x => new { x.DocumentId, x.UserId });
    }

    private void TrackActionsAt()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.GetType()
                    .GetProperty("CreatedAt")?.SetValue(entry.Entity, DateTime.UtcNow);

            if (entry.State == EntityState.Modified)
                entry.Entity.GetType()
                    .GetProperty("UpdatedAt")?.SetValue(entry.Entity, DateTime.UtcNow);
        }
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        TrackActionsAt();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
```

### DbContext konfiguratsiyasi (Dependencies.cs)

```csharp
private static IServiceCollection ConfigureDbContext(
    this IServiceCollection services, IConfiguration configuration)
{
    var dataSource = new NpgsqlDataSourceBuilder(
        configuration.GetConnectionString("DefaultConnectionString"))
        .EnableDynamicJson()
        .Build();

    services.AddDbContext<AppDbContext>(options =>
        options
            .UseNpgsql(dataSource)
            .UseSnakeCaseNamingConvention());

    return services;
}
```

### Broker Pattern

```csharp
// Brokers/Bank/IBankBroker.cs
public interface IBankBroker
{
    Task<Result<LoanStatusResponse>> GetLoanStatusAsync(string loanId, CancellationToken cancellationToken);
    Task<Result> SendPaymentAsync(PaymentRequest request, CancellationToken cancellationToken);
}

// Brokers/Bank/BankBroker.cs
public class BankBroker(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IBankBroker
{
    public async Task<Result<LoanStatusResponse>> GetLoanStatusAsync(
        string loanId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/loans/{loanId}/status", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Error.Failure("Bank.GetLoanStatus.Failed");

        var data = await response.Content.ReadFromJsonAsync<LoanStatusResponse>(cancellationToken);
        return data!;
    }
}
```

---

## 7. API Layer

**Joylashuvi:** `[Project].Api/`

### Fayl tuzilmasi

```
Api/
├── Controllers/
│   ├── Common/
│   │   ├── AuthorizedController.cs   ← Base [Authorize] controller
│   │   └── AdminController.cs        ← Admin rolga mo'ljallangan base
│   ├── Public/
│   │   └── [Names]Controller.cs
│   └── Admin/
│       └── [Names]Controller.cs
├── Extensions/
│   └── ResultExtensions.cs           ← ToProblemDetails()
├── Filters/
│   ├── ModelValidationFilter.cs
│   ├── EnumSchemaFilter.cs
│   ├── EnumOperationFilter.cs
│   ├── HangfireAuthorizationFilter.cs
│   └── SwaggerGroupAttribute.cs
├── Middlewares/
│   └── GlobalExceptionHandlerMiddleware.cs
├── Program.cs
├── Dependencies.cs
└── GlobalUsings.cs
```

### Base Controller

```csharp
[ApiController]
[Authorize]
public abstract class AuthorizedController : ControllerBase
{
    protected long UserId
    {
        get
        {
            var raw = HttpContext.User.FindFirstValue(CustomClaims.Id)
                ?? throw new UnauthorizedException("Required claim not found");
            return (long)Convert.ChangeType(raw, typeof(long));
        }
    }
}

public abstract class AdminController : AuthorizedController { }
```

### Controller — IResult pattern

```csharp
[ApiController]
[Route("api/documents")]
[SwaggerGroup("public")]
public class DocumentsController(IDocumentService documentService) : AuthorizedController
{
    /// <summary>
    /// Get all documents.
    /// </summary>
    /// <param name="query">Filter parameters</param>
    /// <param name="cancellationToken"></param>
    /// <returns>List of documents</returns>
    [HttpGet]
    public async Task<IResult> GetAllAsync(
        [FromQuery] DocumentFilterRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = await documentService.GetAllAsync(query, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Get document by id.
    /// </summary>
    /// <param name="id">Document identifier</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Document details</returns>
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await documentService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Create new document.
    /// </summary>
    /// <param name="request">Document data</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IResult> AddAsync(
        [FromBody] CreateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await documentService.AddAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Update document.
    /// </summary>
    /// <param name="id">Document identifier</param>
    /// <param name="request">Updated data</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(
        long id,
        [FromBody] CreateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await documentService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Delete document.
    /// </summary>
    /// <param name="id">Document identifier</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await documentService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
```

### IResult qoidalari

| Holat | Return |
|-------|--------|
| Ma'lumot bilan muvaffaqiyat | `Results.Ok(result.Data)` |
| Ma'lumotsiz muvaffaqiyat | `Results.Ok()` |
| Yaratildi | `Results.Created(uri, result.Data)` |
| Xato | `result.ToProblemDetails()` |

**`ToProblemDetails()` mapping:**

| ErrorType | HTTP Status |
|-----------|-------------|
| `Validation` | 400 Bad Request |
| `NotFound` | 404 Not Found |
| `Conflict` | 409 Conflict |
| `Failure` | 500 Internal Server Error |

### XML Comment qoidasi (faqat API layer)

```csharp
/// <summary>
/// Bir jumla, inglizcha — endpoint nima qilishini tavsifla.
/// </summary>
/// <param name="id">Resource identifier</param>
/// <param name="request">Request body tavsifi</param>
/// <param name="cancellationToken"></param>
/// <returns>Natija tavsifi yoki bo'sh</returns>
```

- **Yoziladi:** faqat public controller metodlarida (Swagger'da ko'rinadi)
- **Yozilmaydi:** service interface/implementatsiyada, domain classlarda

### GlobalExceptionHandlerMiddleware

```csharp
public class GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception");

        var problemDetails = new ProblemDetails
        {
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
```

---

## 8. Cheatsheetlar

### ✅ Yangi Feature (Entity + CRUD)

```
1.  Domain/Entities/[Entity].cs
        → AuditableModelBase<long> dan inherit

2.  Infrastructure/Persistence/AppDbContext.cs
        → DbSet<Entity> qo'sh

3.  Migration yaratish
        → dotnet ef migrations add Add_[Entity]Migration

4.  Application/Services/[Entity]/[Entity]Errors.cs
        → static Error konstantalar (NotFound, AlreadyExists va h.k.)

5.  Application/Services/[Entity]/Contracts/[Entity]Request.cs
6.  Application/Services/[Entity]/Contracts/[Entity]Response.cs
7.  Application/Services/[Entity]/Contracts/[Entity]FilterRequest.cs  (kerak bo'lsa)
8.  Application/Services/[Entity]/Contracts/[Entity]RequestValidator.cs

9.  Application/Services/[Entity]/I[Entity]Service.cs
10. Application/Services/[Entity]/[Entity]Service.cs

11. Application/Dependencies.cs
        → .AddScoped<I[Entity]Service, [Entity]Service>()

12. Api/Controllers/[Scope]/[Entity]sController.cs
        → AuthorizedController yoki AdminController
        → Har metod ustida XML comment
```

### ✅ Yangi Background Job

```
1. Application/Jobs/[Category]/I[Name]Job.cs
        → metod signaturelari, Result qaytarsin

2. Application/Jobs/[Category]/[Name]Job.cs
        → implementation

3. Application/Jobs/JobsRegistrar.cs
        → RecurringJob.AddOrUpdate<I[Name]Job>() qo'sh (cron expression)

4. Application/Dependencies.cs
        → .AddScoped<I[Name]Job, [Name]Job>()
```

### ✅ Yangi External Integration (Broker)

```
1. Infrastructure/Brokers/[System]/I[System]Broker.cs
        → metod interfacelari, Result qaytarsin

2. Infrastructure/Brokers/[System]/[System]Broker.cs
        → HttpClient yoki SDK orqali implementation

3. Infrastructure/Brokers/[System]/Contracts/
        → [Name]Request.cs, [Name]Response.cs

4. Infrastructure/Dependencies.cs
        → .AddScoped<I[System]Broker, [System]Broker>()
        → HttpClient konfiguratsiyasi (agar kerak bo'lsa)
```

### 🐛 Bug Fix Tekshiruvi

| Tekshirish | Nima qilish kerak |
|-----------|------------------|
| Exception throw? | `Result.Failure(error)` qaytarilishi kerak |
| Null check? | `if (entity is null) return *Errors.NotFound;` |
| Read query'da tracking? | `AsNoTracking()` qo'sh |
| `First` vs `Single`? | `SingleOrDefaultAsync` ishlatilsin |
| Soft delete filter? | `.Where(x => !x.IsDeleted)` qo'sh |
| SaveChanges chaqirilganmi? | Update/Delete dan keyin `SaveChangesAsync()` |

### ♻️ Refactor

| Muammo | Yechim |
|--------|--------|
| Magic string `"Document.NotFound"` | `DocumentErrors.NotFound` static constant |
| Controller ichida validation logikasi | `AbstractValidator<T>` classga ko'chir |
| Controller ichida biznes logika | Service metodiga ko'chir |
| `dbContext` controller'da inject | Service inject qilib DbContext'ni service'da ishlatsin |
| Takrorlanuvchi `null` check | Helper metod yoki `SingleOrDefaultAsync` + early return |
| `int` instead of `long` for Id | `AuditableModelBase<long>` — `long` ishlatilsin |

---

## 9. Tayyor Implementatsiyalar (Reuse uchun)

| Narsa | Qayerda | Nima qiladi |
|-------|---------|-------------|
| `Result<T>` / `Result` | `Domain/Abstractions/Result.cs` | Railway error handling |
| `Error` + `ErrorType` | `Domain/Abstractions/Error.cs` | Error kodi + type |
| `ModelBase<T>` | `Domain/Entities/Common/ModelBase.cs` | Id + IsDeleted |
| `AuditableModelBase<T>` | `Domain/Entities/Common/AuditableModelBase.cs` | + timestamps |
| `PagedList<T>` | `Domain/Abstractions/DataQueryRequest.cs` | Paginatsiya wrapper |
| `DataQueryRequest` | `Domain/Abstractions/DataQueryRequest.cs` | Filter/sort base DTO |
| `AppDbContext.TrackActionsAt()` | `Infrastructure/Persistence/AppDbContext.cs` | Auto audit trail |
| `ToProblemDetails()` | `Api/Extensions/ResultExtensions.cs` | Result → IResult mapping |
| `AuthorizedController` | `Api/Controllers/Common/` | JWT user extraction |
| `GlobalExceptionHandlerMiddleware` | `Api/Middlewares/` | Unhandled exception → 500 |
| `ModelValidationFilter` | `Api/Filters/` | Model state → 400 |
| `QueryableExtensions` | `Infrastructure/Extensions/` | Dynamic filtering |
