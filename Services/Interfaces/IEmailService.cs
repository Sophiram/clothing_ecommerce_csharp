using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainText = null);
        Task<(bool Success, string Message)> SendTestEmailAsync(string toEmail);
        Task<bool> SendOrderConfirmationEmailAsync(Order order);
        Task<bool> SendOrderConfirmationByIdAsync(Guid orderId);
        Task<bool> SendAdminOrderNotificationAsync(Order order);
        Task<bool> SendPaymentReceiptEmailAsync(Payment payment, Order order);
        Task<bool> SendPaymentReceiptByIdAsync(Guid paymentId);
        Task<bool> SendContactInquiryCustomerConfirmationAsync(string name, string email, string? phone, string subject, string message);
        Task<bool> SendContactInquiryAdminNotificationAsync(string name, string email, string? phone, string subject, string message);
        Task<bool> SendLowStockAlertEmailAsync(string productName, string variantSku, string size, string color, int currentStock);
        Task<bool> SendShipmentStatusEmailAsync(Shipment shipment, Order order);
        SmtpSettings GetSettings();
    }
}
