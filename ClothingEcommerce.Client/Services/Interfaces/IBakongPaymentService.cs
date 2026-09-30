using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class CreateBakongPaymentRequest
    {
        public Guid OrderId { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class BakongPaymentResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? PaymentId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public string? QrCode { get; set; }
        public decimal Amount { get; set; }
        public decimal KhrAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? Reference { get; set; }
        public string? Md5 { get; set; }
        public string? MerchantName { get; set; }
        public string? BakongAccountId { get; set; }
        public string? AbaDeepLink { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int ExpiresInSeconds { get; set; }
        public string Status { get; set; } = "PENDING";
    }

    public class BakongStatusResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; } = "PENDING"; // "PENDING", "PAID", "EXPIRED", "CANCELLED", "FAILED"
        public bool IsPaid { get; set; }
        public Guid? PaymentId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? TransactionId { get; set; }
        public string? Reference { get; set; }
        public DateTime? PaidAt { get; set; }
        public int ExpiresInSeconds { get; set; }
        public string? Message { get; set; }
    }

    public interface IBakongPaymentService
    {
        Task<BakongPaymentResponse> CreatePaymentAttemptAsync(Guid orderId, string currency = "USD", string? userId = null);
        Task<BakongStatusResponse> GetPaymentStatusAsync(Guid paymentId);
        Task<BakongStatusResponse> CancelPaymentAsync(Guid paymentId);
        Task<BakongStatusResponse> ConfirmSimulatedPaymentAsync(Guid paymentId);
    }
}
