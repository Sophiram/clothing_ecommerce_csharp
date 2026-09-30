using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class PaymentStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
    }

    public interface IPaymentService
    {
        // Payment Methods
        Task<List<PaymentMethod>> GetActivePaymentMethodsAsync();
        Task<List<PaymentMethod>> GetAllPaymentMethodsAsync(string? search = null);
        Task<PaymentMethod?> GetPaymentMethodByIdAsync(Guid id);
        Task<ServiceResult> CreatePaymentMethodAsync(PaymentMethod method);
        Task<ServiceResult> UpdatePaymentMethodAsync(PaymentMethod method);
        Task<ServiceResult> DeletePaymentMethodAsync(Guid id);
        Task<ServiceResult> TogglePaymentMethodStatusAsync(Guid id);

        // Payments
        Task<(List<Payment> Payments, PaymentStatsDto Stats)> GetPaymentsAsync(string? search, string? status, Guid? paymentMethodId);
        Task<Payment?> GetPaymentByIdAsync(Guid id);
        Task<ServiceResult> CreatePaymentAsync(Payment payment);
        Task<ServiceResult> UpdatePaymentAsync(Payment payment);
        Task<ServiceResult> DeletePaymentAsync(Guid id);
        Task<(List<Order> OrdersWithoutPayment, List<PaymentMethod> ActiveMethods)> GetPaymentCreateSelectListsAsync();
        Task<List<PaymentMethod>> GetPaymentMethodsSelectListAsync();
        Task<List<Order>> GetAllOrdersAsync();
    }
}
