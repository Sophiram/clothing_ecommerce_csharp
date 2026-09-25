using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface ITelegramService
    {
        Task<bool> SendOrderNotificationAsync(Order order);
        Task<bool> SendOrderNotificationByIdAsync(Guid orderId);
        Task<bool> SendPaymentNotificationAsync(Guid paymentId);
        Task<bool> SendContactInquiryNotificationAsync(string name, string email, string? phone, string subject, string message);
        Task<bool> SendTestNotificationAsync(string botToken, string chatId);
        Task<bool> SendBroadcastAsync(string message);
        Task<TelegramSettings> GetSettingsAsync();
        Task<bool> SaveSettingsAsync(TelegramSettings settings);
    }
}
