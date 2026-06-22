# Edit-until-paid cart + one unpaid order at a time

## Context

Today the order flow is **one cart per user → one active order**:
- `Cart` is a single per-user shopping cart (`CartService.FindCartWithItemsAsync` = first cart by `UserId`).
- `POST /api/orders` (`OrderService.CreateDraftAsync`) snapshots that cart into a **Draft** order (reusing the single existing Draft/New order if present).
- `CartService` blocks edits via `HasActiveOrderAsync` (`Cart.CheckoutInProgress`) the moment an order reaches `New`.
- Hamkor webhook (`CheckoutService.HandleCallbackAsync`) confirms payment, then clears the user's cart.

The user wants:
1. **Edit the active order's cart until it is paid** — add/remove items while status is `Draft` or `New` (locked only at `Payed`).
2. **Multiple "active" orders** — i.e. a user can have several orders being fulfilled (`Payed` → `Delivered`).
3. **Create a new order after the previous is paid** — placing/paying must not permanently block new orders.
4. **At most ONE unpaid order (`Draft`/`New`) at a time** — to create a *new* order, the previous one must be **at least `Payed`**.

So the unpaid/checkout stage is a **single, editable order**; once paid it joins the set of in-fulfillment orders and a fresh one can begin.

Decisions confirmed:
- Each order owns its own `Cart` (1:1 via `Order.CartId`); paid orders keep their cart, the user gets a fresh open cart.
- Cart editable in **Draft + New**, locked at **Payed**.
- On cart change, the active draft's `OrderItem` snapshots + `Subtotal`/`TotalAmount` **auto re-sync**.

**Prerequisite (build is currently broken):** the WIP edit to `Domain/Enums/OrderStatus.cs` removed `Accepted` and renumbered values (`Payed = 5` is now "payment confirmed"), but `CheckoutService` and `OrderStatusTransitions` still reference `OrderStatus.Accepted`.

---

## 1. Prerequisite — finish the `Accepted → Payed` rename

- `Application/Services/Checkout/CheckoutService.cs` (~131, ~134): `OrderStatus.Accepted` → `OrderStatus.Payed`.
- `Application/Services/Orders/OrderStatusTransitions.cs` (~15, ~26): `OrderStatus.Accepted` → `OrderStatus.Payed`.
- `grep` to confirm no other `OrderStatus.Accepted` refs.

**Data migration** (enum stored as int; renumbered with an old/new `5` collision → single `CASE` so each branch sees the original value). In the new migration `Up()`, raw SQL on `orders.orders` **and** `orders.order_status_histories`:
```sql
UPDATE orders.orders SET status = CASE status
  WHEN 1 THEN 5  WHEN 2 THEN 5   -- old Accepted, old Payed → Payed(5)
  WHEN 3 THEN 10 WHEN 4 THEN 15  WHEN 5 THEN 20  WHEN 6 THEN 25
  WHEN 7 THEN 30 WHEN 8 THEN 35  WHEN 9 THEN 40  WHEN 10 THEN 45
  ELSE status END;  -- Draft(-1), New(0) unchanged
```

---

## 2. Data model — open vs. consumed cart

**`Domain/Entities/Orders/Cart.cs`** — add:
```csharp
public bool IsCheckedOut { get; set; }   // false = open shopping cart; true = attached to an order
```
- **Open cart** = `Carts.Where(c => c.UserId == userId && !c.IsDeleted && !c.IsCheckedOut)` (get-or-create; the cart used before an order exists).
- `Order.CartId` already links order→cart 1:1.

Migration `Add_Cart_IsCheckedOut` — also carries the §1 enum data-migration SQL.

---

## 3. The single "active cart" + re-sync (core behaviour)

The user always edits **one** cart. Resolve it as:
- If the user has a **Draft/New** order → that order's `Cart` (editing it re-syncs the order).
- Else → the **open** cart (get-or-create, `IsCheckedOut == false`).

`Application/Services/Carts/CartService.cs`:
- Add `ResolveActiveCartAsync(userId)` → returns the active `Cart` **and** its linked Draft/New `Order` (or null). `GetOrCreateAsync`, `AddItemAsync`, `UpdateItemAsync`, `RemoveItemAsync` all operate on this.
- **Remove** `HasActiveOrderAsync` + all `CheckoutInProgress` returns and `CartResponse.IsLocked` (the active cart is by definition never paid; once paid it stops being the active cart).
- After any add/update/remove, **if the active cart belongs to a Draft/New order**: re-sync that order via the shared helper below, and **if its status is `New`, revert to `Draft`** (the prior Hamkor amount is stale → re-initiate required).
- Pricing display already wired via `IProductPricingService`.

**Shared re-sync helper** — extract the snapshot logic currently inside `CreateDraftAsync` into a reusable method (e.g. static `OrderItemSync.Apply(order, cart, pricing)` or a small internal service) used by both `CartService` and `OrderService`:
```
// soft-delete order.Items; rebuild from cart.Items using pricing.CalculateSoumPrice
// (PriceSnap/TotalSnap); recompute Subtotal + TotalAmount (+ DeliveryCost)
```
`CartService` already injects `IProductPricingService` for `CreateContextAsync`.

---

## 4. OrderService — one draft, multiple paid, create-after-paid

`Application/Services/Orders/OrderService.cs`:

**Create (`CreateDraftAsync`):**
- **Guard:** if the user has any `Draft`/`New` order → `OrderErrors.ActiveOrderExists` (must pay/finish it first). This enforces "one unpaid at a time".
- Else: load the **open** cart (non-empty, else `CartEmpty`), create a Draft order with `CartId = openCart.Id`, set `openCart.IsCheckedOut = true`, snapshot items via the shared helper.
- **Remove** the old "reuse existing Draft/New order" branch.
- After a previous order is `Payed`, the open cart resolves fresh → create works again (requirement 3).

**Active orders / cancel:**
- Replace `GetMyDraftAsync` with `GetActiveOrdersAsync(userId)` → all non-terminal orders (`Draft, New, Payed, Preparing, Shipped, InTransit, OutForDelivery`) — the "active" set.
- `CancelMyDraftAsync` → `CancelDraftAsync(userId, orderId)` for the single Draft/New order.
- Add `OrderErrors.ActiveOrderExists` (Conflict `Order.ActiveOrderExists`).

---

## 5. Checkout — remove user-cart clearing

`Application/Services/Checkout/CheckoutService.cs`:
- `InitiatePaymentAsync` stays `Draft`-only (cart edits in `New` revert to `Draft`, so re-initiation re-prices correctly).
- In `HandleCallbackAsync`, **remove `ClearCartAsync(order.UserId)`** — the paid order keeps its own cart (locked by status); the user's next open cart is created fresh on demand. Delete `ClearCartAsync`.

---

## 6. Controllers / endpoints

- `CartsController` — **shape unchanged**; now edits the active cart (open or the single draft) and re-syncs the draft. No more lock. No separate order-item endpoints needed.
- `Api/Controllers/Orders/OrdersController.cs`:
  - `GET /api/orders/active` → `GetActiveOrdersAsync` (replaces `GET /api/orders/draft`).
  - `DELETE /api/orders/{id:long}` → `CancelDraftAsync` (replaces `DELETE /api/orders/draft`).

---

## 7. Frontend impact (note — backend-first; separate follow-up)

`vendly-client` assumes single cart + single draft (`/api/orders/draft`, cart `IsLocked`). After this it should: keep using `/api/carts` (edits resolve to the active draft automatically), list in-fulfillment orders via `/api/orders/active`, and allow a new order once the prior is paid. Out of scope unless requested.

---

## 8. Verification

1. `dotnet build` — `Accepted` errors gone.
2. `dotnet ef database update` — `is_checked_out` added; existing statuses remapped (check one order before/after).
3. `POST /api/carts/items` → `GET /api/carts` shows items (open cart).
4. `POST /api/orders` → Draft created from open cart.
5. `POST /api/carts/items` again → edits the **same draft's** cart; `GET /api/orders/{id}` shows re-synced `Subtotal`/`TotalAmount`.
6. `POST /api/orders` again → `Order.ActiveOrderExists` (draft still unpaid).
7. `POST /api/orders/{id}/payment` → `New`; then `POST /api/carts/items` → succeeds and order reverts to `Draft`.
8. Simulate webhook confirm → `Payed`; `GET /api/carts` now returns a **fresh empty** open cart; `POST /api/orders` → new Draft (requirement 3 ✓).
9. `GET /api/orders/active` lists the paid order + the new draft.
10. `dotnet test` — fix `CartServiceTests` (lock removed); add coverage for one-draft guard + edit-until-paid + create-after-paid.
