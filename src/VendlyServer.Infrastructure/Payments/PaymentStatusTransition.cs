using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Infrastructure.Payments;

// Payment/Order holatining yagona o'tish nuqtasi — Payme/Click provider'lar shu yerdan o'tadi,
// shunda Hamkor bilan bir xil fulfillment oqimi (Order.Status = Payed) saqlanadi.
// Faqat mutatsiya qiladi; SaveChangesAsync chaqiruvchi provider'da.
public static class PaymentStatusTransition
{
    // To'lov muvaffaqiyatli: Payment -> Paid, Order -> Payed + StatusHistory. payment.Order yuklangan bo'lishi shart.
    public static void MarkPaid(Payment payment, string providerTransactionId, string note)
    {
        if (payment.Status == PaymentStatus.Paid) return;

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        payment.TransactionId = providerTransactionId;

        var order = payment.Order;
        order.Status = OrderStatus.Payed;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Payed,
            Note = note,
        });
    }

    // To'lov bekor/muvaffaqiyatsiz: Payment -> Failed, Order -> Draft (qayta urinish uchun ochiq).
    // payment.Order yuklangan bo'lishi shart.
    public static void MarkFailed(Payment payment)
    {
        if (payment.Status == PaymentStatus.Paid) return;

        payment.Status = PaymentStatus.Failed;

        // Faqat New holatidagi orderni Draft'ga qaytaramiz — bu to'lov urinishi davomidagi typik holat.
        // Payed/Shipped/Cancelled holatlarga tegmaymiz (allaqachon yakunlangan oqimlar).
        // Draft'ga qaytsa, foydalanuvchi /payment'ni qayta chaqirishi mumkin (o'sha yoki boshqa
        // provider bilan). InitiatePaymentAsync mavjud Payment'ni qayta tiklab ishlatadi.
        var order = payment.Order;
        if (order.Status != OrderStatus.New) return;

        order.Status = OrderStatus.Draft;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Draft,
            Note = "Payment failed — reverted to draft for retry",
        });
    }
}
