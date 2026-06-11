namespace VendlyServer.Domain.Enums;

// Draft(-1) → checkout boshlanganda
// New(0)   → Hamkor payment initiated
// Accepted(1) → Hamkor webhook: payment confirmed
// Preparing(3) → admin qo'lda
// Shipped(4)   → admin: BTS ga jo'natildi
// InTransit(5) / OutForDelivery(6) / Delivered(7) → BTS webhook
// Cancelled(8) → admin yoki foydalanuvchi (Draft..Shipped gacha)
// ReturnRequested(9) / Returned(10) → qaytarish oqimi
public enum OrderStatus
{
    Draft          = -1,
    New            = 0,
    Accepted       = 1,
    Payed          = 2,
    Preparing      = 3,
    Shipped        = 4,
    InTransit      = 5,
    OutForDelivery = 6, 
    Delivered      = 7,
    Cancelled      = 8,
    ReturnRequested= 9,
    Returned       = 10,
}
