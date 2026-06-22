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

    // To'lov bekor/muvaffaqiyatsiz: faqat Payment -> Failed. Order tegilmaydi (qayta urinishga ochiq).
    public static void MarkFailed(Payment payment)
    {
        if (payment.Status == PaymentStatus.Paid) return;
        payment.Status = PaymentStatus.Failed;
    }
}
