# Category Pricing + dynamic price markup

## Context

`ProductVariant.Price` is stored as a **USD base price** (from Smartup sync / admin). Today every product GET returns this raw USD number directly as if it were the final price. The business needs the storefront to show **soum prices** that are:

1. converted from USD → soum via the live CBU rate, then
2. marked up by a **per-category** rule (percent **or** a fixed soum amount), with a **start/end date** window, then
3. **rounded up** ("yaxshilash") to a configurable step.

When a category has no active rule, a **global default** (default markup % + default rounding step) from `appsettings.json` is used. The same convert → markup → round transform must be applied everywhere a price is returned to the customer (product list, search, detail, cart) and captured at checkout (order snapshot).

This requires: (a) a new **CategoryPrice** entity + CRUD API, (b) global default options, (c) a central **pricing service**, and (d) wiring that service into all customer-facing price surfaces.

Decisions confirmed with the user:
- Global default lives in **appsettings.json** (`IOptions`), not DB.
- Fixed-type value is **additive** markup (`soum + fixedValue`), same as percent.
- Rounding = **ceil to step** (`Math.Ceiling(v / step) * step`).
- Scope = **all product GETs + cart + order snapshot**. The detail endpoint is treated as a display surface (returns transformed price).

---

## 1. Domain — enum + entity

**`Domain/Enums/PriceMarkupType.cs`**
```csharp
public enum PriceMarkupType { Percent, Fixed }
```

**`Domain/Entities/Catalogs/CategoryPrice.cs`** — `AuditableModelBase<long>`, table `catalogs.category_prices`:
```csharp
public long CategoryId { get; set; }
public PriceMarkupType MarkupType { get; set; }     // Percent yoki Fixed
[Column(TypeName = "decimal(18,2)")] public decimal Value { get; set; }  // percent (e.g. 15) yoki soum miqdori
[Column(TypeName = "decimal(18,2)")] public decimal? RoundingStep { get; set; } // null → default
public DateTime? StartDate { get; set; }
public DateTime? EndDate { get; set; }
[ForeignKey(nameof(CategoryId))] public Category Category { get; set; } = null!;
```
Add `public ICollection<CategoryPrice> Prices { get; set; } = [];` to `Category.cs`.

**`Infrastructure/Persistence/AppDbContext.cs`** — add `public DbSet<CategoryPrice> CategoryPrices { get; set; }`.

Migration: `dotnet ef migrations add Add_CategoryPrice` (from `src/VendlyServer.Infrastructure`, startup project `../VendlyServer.Api`).

A category may have multiple rows (scheduled windows). "Active" = `!IsDeleted && (StartDate == null || StartDate <= now) && (EndDate == null || EndDate >= now)`. If several match, pick the latest by `StartDate` then `CreatedAt`.

---

## 2. Global default — Options (appsettings.json)

**`Application/Services/Pricing/PricingOptions.cs`** (mirror `CurrencyApiOptions` + `CurrencyApiOptionsSetup`):
```csharp
public const string SectionName = "Pricing";
public decimal DefaultMarkupPercent { get; set; } = 0;   // default foiz
public decimal DefaultRoundingStep  { get; set; } = 0;   // 0 → rounding off
```
Add `PricingOptionsSetup : IConfigureOptions<PricingOptions>` + register via `services.ConfigureOptions<PricingOptionsSetup>()` in `Dependencies.ConfigureApplication`.

Add to **all** `appsettings*.json`:
```json
"Pricing": { "DefaultMarkupPercent": 0, "DefaultRoundingStep": 1000 }
```

---

## 3. Pricing service (core logic)

**`Application/Services/Pricing/`**: `IProductPricingService`, `ProductPricingService`, `PricingContext`, `PricingErrors`.

`ProductPricingService(ICurrencyConverterService currency, AppDbContext db, IOptions<PricingOptions> options)`:

```csharp
Task<Result<PricingContext>> CreateContextAsync(CancellationToken ct)
```
- `currency.GetUsdRateAsync(ct)` → if fail, return `PricingErrors.RateUnavailable`.
- Load all active `CategoryPrice` rows (`AsNoTracking`, the active-window filter above), reduce to one rule per `CategoryId`.
- Return `PricingContext(usdRate, rulesByCategoryId, options.Value)`.

`PricingContext.CalculateSoumPrice(decimal usdPrice, long categoryId)`:
```csharp
var soum = usdPrice * _usdRate;
var rule = _rules.GetValueOrDefault(categoryId);
decimal markup = rule is null
    ? soum * _defaults.DefaultMarkupPercent / 100m
    : rule.MarkupType == PriceMarkupType.Percent ? soum * rule.Value / 100m : rule.Value;
var final = soum + markup;
var step = rule?.RoundingStep ?? _defaults.DefaultRoundingStep;
return step > 0 ? Math.Ceiling(final / step) * step : final;
```

Loading the rate + rules **once per request** (context) avoids N CBU calls / N queries when mapping a list. Register `IProductPricingService` scoped in `Dependencies`.

`PricingErrors.RateUnavailable = Error.Failure("Pricing.RateUnavailable")`.

**Failure handling:** money path (checkout) must **hard-fail**; display paths fall back to raw price + a warning log (see §5).

---

## 4. CategoryPrice CRUD feature (12-step backend pattern)

`Application/Services/CategoryPrices/`:
- `CategoryPriceErrors.cs` — `NotFound`, `CategoryNotFound`.
- `Contracts/CreateCategoryPriceRequest.cs` (CategoryId, MarkupType, Value, RoundingStep?, StartDate?, EndDate?) + `CreateCategoryPriceRequestValidator` (Value ≥ 0; if Percent, Value ≤ some sane max; EndDate ≥ StartDate when both set; category exists check stays in service).
- `Contracts/UpdateCategoryPriceRequest.cs` + validator.
- `Contracts/CategoryPriceResponse.cs` (Id, CategoryId, MarkupType, Value, RoundingStep, StartDate, EndDate, timestamps).
- `ICategoryPriceService` + `CategoryPriceService` — full CRUD following `CategoryService`/`ProductService` conventions (`AsNoTracking` reads, soft-delete via `IsDeleted`, `SaveChangesAsync` only in service, `Result` returns, validate `CategoryId` exists on create/update).
- Register in `Dependencies`.

**`Api/Controllers/Catalog/CategoryPricesController.cs`** (`[Route("api/category-prices")]`, `[Authorize(Roles = "Admin,Manager")]`, XML comments) — GET all (optional `?categoryId=`), GET by id, POST, PUT, DELETE. Mirror `CategoriesController`.

---

## 5. Apply transform at every price surface

Inject `IProductPricingService` into `ProductService`, `CartService`, `OrderService`. Build the context once at the start of each read/checkout.

**`ProductService.GetAllAsync`** (`ProductCardResponse.MinPrice`): after `ToListAsync`, build context; map `MinPrice = ctx.CalculateSoumPrice((decimal)defaultVariant.Price, p.CategoryId)`. On context failure: log warning, leave raw price (method returns `PagedList`, not `Result`).

**`ProductService.SearchAsync`**: change the SQL projection to select an intermediate (include `p.CategoryId` + raw min price), materialize, then map to `ProductSearchResponse` applying `CalculateSoumPrice` in memory. Same soft-fallback on context failure.

**`ProductService.GetByIdAsync`** (`ProductAdminDetailResponse` → each `ProductVariantResponse.Price`): materialize is already in-DB projection; restructure to fetch raw, then map variants applying `CalculateSoumPrice(v.Price, p.CategoryId)`. (Per user: detail endpoint returns the transformed display price.)
> ⚠️ Admin-edit caveat: this endpoint is also used by the admin panel. Admin variant-edit forms must treat the variant base price as raw USD and **not** re-submit the transformed value via `BulkUpdate`/`UpdateVariant`. Flagged for confirmation during implementation; the write endpoints themselves are unchanged (they accept raw USD).

**`CartService.MapToResponse`**: convert from `static` to an instance method (or pass `PricingContext`); apply `CalculateSoumPrice(i.ProductVariant.Price, i.ProductVariant.Product.CategoryId)` per item; `TotalAmount` then sums transformed prices. `Product` is already `Include`d (CategoryId available). Build context in the public read method(s) that call `MapToResponse`. Soft-fallback to raw on failure.

**`OrderService.CreateOrderAsync`** (money path — both the existing-draft branch and the new-order branch, lines ~82 & ~129): build context at method start; **if context fails, return `PricingErrors.RateUnavailable`** (no silent raw fallback). Set `PriceSnap = ctx.CalculateSoumPrice(variant.Price, variant.Product.CategoryId)` and `TotalSnap = PriceSnap * Qty`; `Subtotal`/`TotalAmount` computed from the transformed snapshot. `variant.Product` is already loaded. Order GETs read the stored snapshot unchanged (no double transform).

No change to RecentlyViewed/Wishlist (they expose no price) or to admin product **list** (`GetAllAdminAsync` has no price).

---

## 6. Files touched (summary)

New: `PriceMarkupType.cs`, `CategoryPrice.cs`, `Pricing/*` (4 files), `CategoryPrices/*` (~7 files), `CategoryPricesController.cs`, migration.
Edited: `Category.cs`, `AppDbContext.cs`, `Dependencies.cs`, `appsettings*.json` (×4), `ProductService.cs`, `CartService.cs`, `OrderService.cs`.

---

## 7. Verification

1. `dotnet build` the solution — no errors.
2. `dotnet ef database update` — `category_prices` table created.
3. Set `Pricing.DefaultRoundingStep=1000`, `DefaultMarkupPercent=10` in `appsettings.Development.json`. Run the API.
4. `GET /api/products` for a category **without** a CategoryPrice → verify `minPrice ≈ ceil(usd * cbuRate * 1.10 / 1000) * 1000`.
5. `POST /api/category-prices` `{ categoryId, markupType: "Percent", value: 25, roundingStep: 5000 }`. Re-GET the same category → price now uses 25% + 5000 step. Repeat with `markupType: "Fixed", value: 30000` → verify additive fixed markup.
6. Set a `startDate`/`endDate` window in the future → verify the rule is ignored (falls back to default) until active.
7. `GET /api/products/{id}`, `GET /api/products/search?q=`, and cart GET → all show transformed soum prices.
8. Add to cart → create order (`checkout`/order draft) → confirm `OrderItem.PriceSnap` is the transformed soum value; re-GET the order → snapshot unchanged.
9. Temporarily break the CBU rate (bad API key) → checkout returns `Pricing.RateUnavailable`; product list logs a warning and degrades to raw (display only).
