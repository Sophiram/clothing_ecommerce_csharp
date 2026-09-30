using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(
            AppDbContext context,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<TelegramService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TelegramSettings> GetSettingsAsync()
        {
            var settings = await _context.TelegramSettings.FirstOrDefaultAsync();
            var envToken = _configuration["TELEGRAM_BOT_TOKEN"] ?? _configuration["TELEGRAM:BotToken"];
            var envChatId = _configuration["TELEGRAM_CHAT_ID"] ?? _configuration["TELEGRAM:ChatId"];

            if (settings == null)
            {
                settings = new TelegramSettings
                {
                    Id = Guid.NewGuid(),
                    BotToken = envToken?.Trim() ?? string.Empty,
                    ChatId = envChatId?.Trim() ?? string.Empty,
                    IsEnabled = true,
                    NotifyOnNewOrder = true,
                    NotifyOnPaymentReceived = true,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.TelegramSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            else
            {
                bool modified = false;
                if (string.IsNullOrWhiteSpace(settings.BotToken) && !string.IsNullOrWhiteSpace(envToken))
                {
                    settings.BotToken = envToken.Trim();
                    modified = true;
                }
                // If ChatId is empty or set to bot username (@khmerclothingstore_bot which is invalid as recipient chat_id)
                if ((string.IsNullOrWhiteSpace(settings.ChatId) || settings.ChatId.Contains("khmerclothingstore_bot", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(envChatId))
                {
                    settings.ChatId = envChatId.Trim();
                    modified = true;
                }
                if (!settings.IsEnabled && !string.IsNullOrWhiteSpace(settings.BotToken) && !string.IsNullOrWhiteSpace(settings.ChatId))
                {
                    settings.IsEnabled = true;
                    modified = true;
                }
                if (modified)
                {
                    await _context.SaveChangesAsync();
                }
            }
            return settings;
        }

        public async Task<bool> SaveSettingsAsync(TelegramSettings settings)
        {
            var existing = await _context.TelegramSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                _context.TelegramSettings.Add(settings);
            }
            else
            {
                existing.BotToken = settings.BotToken?.Trim() ?? string.Empty;
                existing.ChatId = settings.ChatId?.Trim() ?? string.Empty;
                existing.IsEnabled = settings.IsEnabled;
                existing.NotifyOnNewOrder = settings.NotifyOnNewOrder;
                existing.NotifyOnPaymentReceived = settings.NotifyOnPaymentReceived;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SendTestNotificationAsync(string botToken, string chatId)
        {
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            {
                return false;
            }

            var message = "<b>✅ TELEGRAM BOT TEST</b>\n\n" +
                          "Congratulations! Your Telegram Bot integration for CLOTHÉ is working perfectly!\n\n" +
                          $"<b>Time:</b> {DateTime.Now:dd MMM yyyy, HH:mm:ss}";

            return await PostMessageToTelegramAsync(botToken, chatId, message);
        }

        public async Task<bool> SendBroadcastAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var settings = await GetSettingsAsync();
            var botToken = !string.IsNullOrWhiteSpace(settings.BotToken) ? settings.BotToken : _configuration["TELEGRAM_BOT_TOKEN"];
            var chatId = (!string.IsNullOrWhiteSpace(settings.ChatId) && !settings.ChatId.Contains("khmerclothingstore_bot", StringComparison.OrdinalIgnoreCase))
                ? settings.ChatId
                : _configuration["TELEGRAM_CHAT_ID"];

            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            {
                _logger.LogWarning("Cannot send broadcast: Telegram credentials missing.");
                return false;
            }

            var formattedMessage = "<b>📢 STORE ANNOUNCEMENT</b>\n\n" +
                                   message.Trim() + "\n\n" +
                                   $"<i>Sent from CLOTHÉ Admin • {DateTime.Now:dd MMM yyyy, HH:mm}</i>";

            return await PostMessageToTelegramAsync(botToken, chatId, formattedMessage);
        }

        public async Task<bool> SendContactInquiryNotificationAsync(string name, string email, string? phone, string subject, string message)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var botToken = !string.IsNullOrWhiteSpace(settings.BotToken) ? settings.BotToken : _configuration["TELEGRAM_BOT_TOKEN"];
                var chatId = (!string.IsNullOrWhiteSpace(settings.ChatId) && !settings.ChatId.Contains("khmerclothingstore_bot", StringComparison.OrdinalIgnoreCase))
                    ? settings.ChatId
                    : _configuration["TELEGRAM_CHAT_ID"];

                if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                {
                    _logger.LogWarning("Telegram contact inquiry skipped: credentials missing.");
                    return false;
                }

                var sb = new StringBuilder();
                sb.AppendLine("<b>📩 NEW CUSTOMER INQUIRY!</b>");
                sb.AppendLine($"<b>From:</b> {name}");
                sb.AppendLine($"<b>Email:</b> {email}");
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    sb.AppendLine($"<b>Phone:</b> {phone}");
                }
                sb.AppendLine($"<b>Subject:</b> {subject}");
                sb.AppendLine();
                sb.AppendLine("<b>Message:</b>");
                sb.AppendLine(message);
                sb.AppendLine();
                sb.AppendLine($"<i>Received at {DateTime.Now:dd MMM yyyy, HH:mm}</i>");

                return await PostMessageToTelegramAsync(botToken, chatId, sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact inquiry to Telegram.");
                return false;
            }
        }

        public async Task<bool> SendPaymentNotificationAsync(Guid paymentId)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var botToken = !string.IsNullOrWhiteSpace(settings.BotToken) ? settings.BotToken : _configuration["TELEGRAM_BOT_TOKEN"];
                var chatId = (!string.IsNullOrWhiteSpace(settings.ChatId) && !settings.ChatId.Contains("khmerclothingstore_bot", StringComparison.OrdinalIgnoreCase))
                    ? settings.ChatId
                    : _configuration["TELEGRAM_CHAT_ID"];

                if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                {
                    _logger.LogWarning("Telegram payment notification skipped: BotToken or ChatId is empty.");
                    return false;
                }

                if (!settings.IsEnabled || !settings.NotifyOnPaymentReceived)
                {
                    _logger.LogInformation("Telegram payment notification skipped: disabled in settings.");
                    return false;
                }

                var payment = await _context.Payments
                    .AsNoTracking()
                    .Include(p => p.PaymentMethod)
                    .Include(p => p.Order)
                        .ThenInclude(o => o.Customer)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    _logger.LogWarning("Payment {PaymentId} not found for Telegram notification.", paymentId);
                    return false;
                }

                var custName = $"{payment.Order?.Customer?.FirstName} {payment.Order?.Customer?.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(custName)) custName = payment.Order?.Customer?.Email ?? "Customer";

                var sb = new StringBuilder();
                sb.AppendLine("<b>💰 PAYMENT RECEIVED / VERIFIED!</b>");
                sb.AppendLine($"<b>Order:</b> <code>#{payment.OrderId.ToString()[..8].ToUpper()}</code>");
                sb.AppendLine($"<b>Amount:</b> <b>${payment.Amount:0.00} {payment.Currency}</b>");
                sb.AppendLine($"<b>Method:</b> {payment.PaymentMethod?.Name ?? "Bakong KHQR"}");
                sb.AppendLine($"<b>Customer:</b> {custName}");
                sb.AppendLine($"<b>Status:</b> {payment.PaymentStatus}");
                if (!string.IsNullOrWhiteSpace(payment.BakongTransactionId))
                {
                    sb.AppendLine($"<b>Tx Hash:</b> <code>{payment.BakongTransactionId}</code>");
                }
                sb.AppendLine($"<b>Paid At:</b> {(payment.PaidAt ?? DateTime.UtcNow).ToLocalTime():dd MMM yyyy, HH:mm}");

                return await PostMessageToTelegramAsync(botToken, chatId, sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram payment notification for payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<bool> SendOrderNotificationByIdAsync(Guid orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.Payment)
                    .ThenInclude(p => p.PaymentMethod)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for Telegram notification.", orderId);
                return false;
            }

            return await SendOrderNotificationAsync(order);
        }

        public async Task<bool> SendOrderNotificationAsync(Order order)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var botToken = !string.IsNullOrWhiteSpace(settings.BotToken) ? settings.BotToken : _configuration["TELEGRAM_BOT_TOKEN"];
                var chatId = (!string.IsNullOrWhiteSpace(settings.ChatId) && !settings.ChatId.Contains("khmerclothingstore_bot", StringComparison.OrdinalIgnoreCase))
                    ? settings.ChatId
                    : _configuration["TELEGRAM_CHAT_ID"];

                if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                {
                    _logger.LogWarning("Telegram order notification skipped: BotToken or ChatId is empty.");
                    return false;
                }

                if (!settings.IsEnabled || !settings.NotifyOnNewOrder)
                {
                    _logger.LogInformation("Telegram order notification skipped: disabled in settings.");
                    return false;
                }

                // Ensure complete details if relations not eagerly loaded
                var fullOrder = order;
                if (order.Items == null || !order.Items.Any() || order.Customer == null)
                {
                    fullOrder = await _context.Orders
                        .AsNoTracking()
                        .Include(o => o.Customer)
                        .Include(o => o.Address)
                        .Include(o => o.Payment)
                            .ThenInclude(p => p.PaymentMethod)
                        .Include(o => o.Shipment)
                        .Include(o => o.Items)
                            .ThenInclude(i => i.Variant)
                                .ThenInclude(v => v.Product)
                        .FirstOrDefaultAsync(o => o.Id == order.Id) ?? order;
                }

                var sb = new StringBuilder();
                sb.AppendLine("<b>🛒 NEW ORDER RECEIVED!</b>");
                sb.AppendLine($"<b>Order ID:</b> <code>#{fullOrder.Id.ToString()[..8].ToUpper()}</code>");
                sb.AppendLine($"<b>Date:</b> {fullOrder.OrderDate.ToLocalTime():dd MMM yyyy, HH:mm}");
                sb.AppendLine();

                // Customer info
                var custName = $"{fullOrder.Customer?.FirstName} {fullOrder.Customer?.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(custName)) custName = fullOrder.Customer?.Email ?? "Guest Customer";
                sb.AppendLine("<b>👤 CUSTOMER DETAILS</b>");
                sb.AppendLine($"• <b>Name:</b> {custName}");
                if (!string.IsNullOrWhiteSpace(fullOrder.Customer?.Phone))
                {
                    sb.AppendLine($"• <b>Phone:</b> {fullOrder.Customer.Phone}");
                }
                if (!string.IsNullOrWhiteSpace(fullOrder.Customer?.Email))
                {
                    sb.AppendLine($"• <b>Email:</b> {fullOrder.Customer.Email}");
                }
                sb.AppendLine();

                // Delivery info
                sb.AppendLine("<b>🚚 DELIVERY & LOGISTICS</b>");
                if (fullOrder.Shipment != null)
                {
                    sb.AppendLine($"• <b>Carrier:</b> {fullOrder.Shipment.ShippingCompany}");
                    if (!string.IsNullOrWhiteSpace(fullOrder.Shipment.TrackingNumber))
                    {
                        sb.AppendLine($"• <b>Tracking #:</b> <code>{fullOrder.Shipment.TrackingNumber}</code>");
                    }
                }
                if (fullOrder.Address != null)
                {
                    sb.AppendLine($"• <b>Address/Branch:</b> {fullOrder.Address.Street}");
                    if (!string.IsNullOrWhiteSpace(fullOrder.Address.City) || !string.IsNullOrWhiteSpace(fullOrder.Address.Province))
                    {
                        sb.AppendLine($"• <b>Location:</b> {fullOrder.Address.City}, {fullOrder.Address.Province}");
                    }
                }
                sb.AppendLine();

                // Items info
                sb.AppendLine("<b>🛍️ ORDER ITEMS</b>");
                if (fullOrder.Items != null && fullOrder.Items.Any())
                {
                    foreach (var item in fullOrder.Items)
                    {
                        var pName = item.Variant?.Product?.Name ?? "Item";
                        sb.AppendLine($"• {pName} (x{item.Quantity}) — <b>${(item.UnitPrice * item.Quantity):0.00}</b>");
                    }
                }
                sb.AppendLine();

                // Payment & Totals
                sb.AppendLine("<b>💵 PAYMENT & TOTALS</b>");
                if (fullOrder.Payment != null)
                {
                    sb.AppendLine($"• <b>Payment Method:</b> {fullOrder.Payment.PaymentMethod?.Name ?? "Online"}");
                    sb.AppendLine($"• <b>Payment Status:</b> {fullOrder.Payment.PaymentStatus}");
                }
                sb.AppendLine($"• <b>TOTAL AMOUNT:</b> <b>${fullOrder.TotalAmount:0.00}</b>");

                return await PostMessageToTelegramAsync(botToken, chatId, sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram order notification for order {OrderId}", order.Id);
                return false;
            }
        }

        private async Task<bool> PostMessageToTelegramAsync(string botToken, string chatId, string textHtml)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{botToken.Trim()}/sendMessage";
                var payload = new
                {
                    chat_id = chatId.Trim(),
                    text = textHtml,
                    parse_mode = "HTML"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully sent Telegram notification to ChatId {ChatId}.", chatId);
                    return true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Telegram API returned error {StatusCode}: {Error}", response.StatusCode, errorBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP exception posting message to Telegram.");
                return false;
            }
        }
    }
}
