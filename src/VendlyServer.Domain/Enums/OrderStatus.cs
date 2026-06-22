namespace VendlyServer.Domain.Enums;

// Draft(-1) → checkout boshlanganda
// New(0)   → Hamkor payment initiated
// Payed(1) → Hamkor webhook: payment confirmed
// Preparing(3) → admin qo'lda
// Shipped(4)   → admin: BTS ga jo'natildi
// InTransit(5) / OutForDelivery(6) / Delivered(7) → BTS webhook
// Cancelled(8) → admin yoki foydalanuvchi (Draft..Shipped gacha)
// ReturnRequested(9) / Returned(10) → qaytarish oqimi
public enum OrderStatus
{
    Draft          = -1,
    New            = 0,
    Payed          = 5,
    Preparing      = 10,
    Shipped        = 15,
    InTransit      = 20,
    OutForDelivery = 25, 
    Delivered      = 30,
    Cancelled      = 35,
    ReturnRequested= 40,
    Returned       = 45,
}
