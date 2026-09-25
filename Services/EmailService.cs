using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration, 
            IServiceScopeFactory scopeFactory,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public SmtpSettings GetSettings()
        {
            // Read MAIL_* keys first (user-provided format), fall back to legacy SMTP_* keys
            var host = Environment.GetEnvironmentVariable("MAIL_HOST")
                ?? _configuration["MAIL_HOST"]
                ?? Environment.GetEnvironmentVariable("SMTP_HOST")
                ?? _configuration["SMTP_HOST"]
                ?? "smtp.gmail.com";

            var portStr = Environment.GetEnvironmentVariable("MAIL_PORT")
                ?? _configuration["MAIL_PORT"]
                ?? Environment.GetEnvironmentVariable("SMTP_PORT")
                ?? _configuration["SMTP_PORT"]
                ?? "587";
            _ = int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            var username = Environment.GetEnvironmentVariable("MAIL_USERNAME")
                ?? _configuration["MAIL_USERNAME"]
                ?? Environment.GetEnvironmentVariable("SMTP_USERNAME")
                ?? _configuration["SMTP_USERNAME"]
                ?? string.Empty;

            var password = Environment.GetEnvironmentVariable("MAIL_PASSWORD")
                ?? _configuration["MAIL_PASSWORD"]
                ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD")
                ?? _configuration["SMTP_PASSWORD"]
                ?? string.Empty;

            var senderEmail = Environment.GetEnvironmentVariable("MAIL_FROM_ADDRESS")
                ?? _configuration["MAIL_FROM_ADDRESS"]
                ?? Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL")
                ?? _configuration["SMTP_FROM_EMAIL"];
            if (string.IsNullOrWhiteSpace(senderEmail)) senderEmail = username;
            if (string.IsNullOrWhiteSpace(senderEmail)) senderEmail = "noreply@clothe.com";

            var senderName = Environment.GetEnvironmentVariable("MAIL_FROM_NAME")
                ?? _configuration["MAIL_FROM_NAME"]
                ?? Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
                ?? _configuration["SMTP_FROM_NAME"]
                ?? "CLOTHÉ Clothing Store";

            // Port 465 → implicit SSL (always on); port 587 → STARTTLS; read MAIL_ENCRYPTION as hint
            var encryptionHint = (Environment.GetEnvironmentVariable("MAIL_ENCRYPTION")
                ?? _configuration["MAIL_ENCRYPTION"]
                ?? "").ToLower();
            bool enableSsl = port == 465 || encryptionHint == "ssl";
            if (!enableSsl)
            {
                var sslStr = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL")
                    ?? _configuration["SMTP_ENABLE_SSL"]
                    ?? "true";
                _ = bool.TryParse(sslStr, out enableSsl);
            }

            var adminEmail = Environment.GetEnvironmentVariable("SMTP_ADMIN_EMAIL")
                ?? _configuration["SMTP_ADMIN_EMAIL"]
                ?? Environment.GetEnvironmentVariable("MAIL_ADMIN_EMAIL")
                ?? _configuration["MAIL_ADMIN_EMAIL"]
                ?? Environment.GetEnvironmentVariable("MAIL_FROM_ADDRESS")
                ?? _configuration["MAIL_FROM_ADDRESS"]
                ?? Environment.GetEnvironmentVariable("MAIL_USERNAME")
                ?? _configuration["MAIL_USERNAME"]
                ?? Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL")
                ?? _configuration["SUPERADMIN_EMAIL"]
                ?? "sornsophiram11@gmail.com";

            var notifyOrderStr = Environment.GetEnvironmentVariable("SMTP_NOTIFY_NEW_ORDER")
                ?? _configuration["SMTP_NOTIFY_NEW_ORDER"]
                ?? "true";
            _ = bool.TryParse(notifyOrderStr, out bool notifyOrder);

            var notifyPayStr = Environment.GetEnvironmentVariable("SMTP_NOTIFY_PAYMENT_SUCCESS")
                ?? _configuration["SMTP_NOTIFY_PAYMENT_SUCCESS"]
                ?? "true";
            _ = bool.TryParse(notifyPayStr, out bool notifyPayment);

            var notifyStockStr = Environment.GetEnvironmentVariable("SMTP_NOTIFY_LOW_STOCK")
                ?? _configuration["SMTP_NOTIFY_LOW_STOCK"]
                ?? "true";
            _ = bool.TryParse(notifyStockStr, out bool notifyStock);

            var notifyShipStr = Environment.GetEnvironmentVariable("SMTP_NOTIFY_SHIPMENT_UPDATE")
                ?? _configuration["SMTP_NOTIFY_SHIPMENT_UPDATE"]
                ?? "true";
            _ = bool.TryParse(notifyShipStr, out bool notifyShipment);

            return new SmtpSettings
            {
                Host = host.Trim(),
                Port = port,
                Username = username.Trim(),
                Password = password.Trim(),
                SenderEmail = senderEmail.Trim(),
                SenderName = senderName.Trim(),
                EnableSsl = enableSsl,
                AdminEmail = adminEmail.Trim(),
                NotifyOnNewOrder = notifyOrder,
                NotifyOnPaymentSuccess = notifyPayment,
                NotifyOnLowStock = notifyStock,
                NotifyOnShipmentUpdate = notifyShipment
            };
        }

        private async Task<MailKit.Net.Smtp.SmtpClient> CreateAndConnectClientAsync(SmtpSettings settings)
        {
            var client = new MailKit.Net.Smtp.SmtpClient
            {
                Timeout = 15000,
                CheckCertificateRevocation = false
            };

            // Allow certificate chain validation in varied hosting / development environments
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            // Support both port 465 (SSL on connect) and port 587 (STARTTLS)
            SecureSocketOptions socketOptions;
            if (settings.Port == 465)
            {
                socketOptions = SecureSocketOptions.SslOnConnect;
            }
            else if (settings.Port == 587)
            {
                socketOptions = SecureSocketOptions.StartTls;
            }
            else
            {
                socketOptions = settings.EnableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
            }

            try
            {
                await client.ConnectAsync(settings.Host, settings.Port, socketOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary SMTP connection to {Host}:{Port} failed. Attempting fallback port...", settings.Host, settings.Port);
                var fallbackPort = settings.Port == 465 ? 587 : 465;
                var fallbackSocket = fallbackPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                await client.ConnectAsync(settings.Host, fallbackPort, fallbackSocket);
            }

            if (!string.IsNullOrWhiteSpace(settings.Username) && !string.IsNullOrWhiteSpace(settings.Password))
            {
                await client.AuthenticateAsync(settings.Username, settings.Password);
            }
            return client;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainText = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Email recipient address was empty.");
                return false;
            }

            var settings = GetSettings();
            if (!settings.IsConfigured)
            {
                _logger.LogWarning("SMTP is not fully configured in environment. Skipped sending email to {Email}", toEmail);
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                if (!string.IsNullOrWhiteSpace(plainText))
                {
                    builder.TextBody = plainText;
                }
                message.Body = builder.ToMessageBody();

                using var client = await CreateAndConnectClientAsync(settings);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Successfully sent email '{Subject}' to {Email}", subject, toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}. Error: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        public async Task<(bool Success, string Message)> SendTestEmailAsync(string toEmail)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return (false, "Please provide a valid destination email address.");
            }

            var settings = GetSettings();
            if (!settings.IsConfigured)
            {
                return (false, "SMTP is not fully configured. Please set MAIL_USERNAME and MAIL_PASSWORD in your .env file.");
            }

            var testHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .card {{ background: #ffffff; max-width: 600px; margin: 0 auto; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }}
        .header {{ background: linear-gradient(135deg, #2563eb, #1d4ed8); color: white; padding: 28px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px; }}
        .badge {{ display: inline-block; padding: 4px 12px; background: rgba(255,255,255,0.2); border-radius: 9999px; font-size: 12px; margin-top: 8px; }}
        .content {{ padding: 28px; font-size: 15px; line-height: 1.6; color: #334155; }}
        .info-box {{ background: #f1f5f9; border-radius: 10px; padding: 16px; margin: 20px 0; }}
        .info-row {{ display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 14px; }}
        .info-row:last-child {{ margin-bottom: 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 13px; color: #94a3b8; border-top: 1px solid #f1f5f9; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1>CLOTHÉ STORE</h1>
            <div class='badge'>SMTP Email Service Test</div>
        </div>
        <div class='content'>
            <p>Hello,</p>
            <p>Congratulations! Your SMTP Mail Notification Service is properly configured and successfully communicating with your mail server.</p>
            <div class='info-box'>
                <div class='info-row'><strong>SMTP Host:</strong> <span>{settings.Host}:{settings.Port}</span></div>
                <div class='info-row'><strong>Sender Name:</strong> <span>{settings.SenderName}</span></div>
                <div class='info-row'><strong>Sender Email:</strong> <span>{settings.SenderEmail}</span></div>
                <div class='info-row'><strong>SSL Enabled:</strong> <span>{(settings.EnableSsl ? "Yes (Secure)" : "No")}</span></div>
                <div class='info-row'><strong>Timestamp:</strong> <span>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</span></div>
            </div>
            <p>Automatic notification features such as order confirmation, payment receipts, and admin alerts are now operational.</p>
        </div>
        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} CLOTHÉ Clothing Ecommerce. All rights reserved.
        </div>
    </div>
</body>
</html>";

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
                message.Subject = "[CLOTHÉ] SMTP Mail Service Connection Test";

                var builder = new BodyBuilder
                {
                    HtmlBody = testHtml
                };
                message.Body = builder.ToMessageBody();

                using var client = await CreateAndConnectClientAsync(settings);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return (true, $"Test email successfully dispatched to {toEmail} via {settings.Host}:{settings.Port}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {Email}", toEmail);
                return (false, $"SMTP Connection Error: {ex.Message}");
            }
        }

        public async Task<bool> SendOrderConfirmationByIdAsync(Guid orderId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var order = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Product)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Size)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Color)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order != null)
                {
                    var cResult = await SendOrderConfirmationEmailAsync(order);
                    var aResult = await SendAdminOrderNotificationAsync(order);
                    return cResult || aResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SendOrderConfirmationByIdAsync for {OrderId}", orderId);
            }
            return false;
        }

        public async Task<bool> SendPaymentReceiptByIdAsync(Guid paymentId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var payment = await db.Payments
                    .AsNoTracking()
                    .Include(p => p.PaymentMethod)
                    .Include(p => p.Order)
                        .ThenInclude(o => o.Customer)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment != null && payment.Order != null)
                {
                    return await SendPaymentReceiptEmailAsync(payment, payment.Order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SendPaymentReceiptByIdAsync for {PaymentId}", paymentId);
            }
            return false;
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(Order order)
        {
            var settings = GetSettings();
            if (!settings.NotifyOnNewOrder) return false;

            var customerEmail = order.Customer?.Email;
            if (string.IsNullOrWhiteSpace(customerEmail)) return false;

            var orderCode = order.Id.ToString()[..8].ToUpper();
            var orderDate = order.OrderDate.ToString("MMM dd, yyyy HH:mm");
            var shippingName = $"{order.Customer?.FirstName} {order.Customer?.LastName}".Trim();

            var itemsHtml = new StringBuilder();
            if (order.Items != null)
            {
                foreach (var item in order.Items)
                {
                    var pName = item.Variant?.Product?.Name ?? "Clothing Item";
                    var size = item.Variant?.Size?.Name ?? "-";
                    var color = item.Variant?.Color?.Name ?? "-";
                    var subtotal = item.UnitPrice * item.Quantity;

                    itemsHtml.Append($@"
                    <tr style='border-bottom: 1px solid #f1f5f9;'>
                        <td style='padding: 12px 8px; font-weight: 500;'>{pName} <span style='font-size: 12px; color: #64748b;'>({size}, {color})</span></td>
                        <td style='padding: 12px 8px; text-align: center; color: #475569;'>{item.Quantity}</td>
                        <td style='padding: 12px 8px; text-align: right; color: #475569;'>${item.UnitPrice:N2}</td>
                        <td style='padding: 12px 8px; text-align: right; font-weight: 600; color: #0f172a;'>${subtotal:N2}</td>
                    </tr>");
                }
            }

            var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; background: #f8fafc; color: #1e293b; margin: 0; padding: 24px;'>
    <div style='background: #ffffff; max-width: 620px; margin: 0 auto; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);'>
        <div style='background: #0f172a; color: white; padding: 28px; text-align: center;'>
            <h1 style='margin: 0; font-size: 22px; font-weight: 800; letter-spacing: 1px;'>CLOTHÉ</h1>
            <p style='margin: 6px 0 0 0; color: #94a3b8; font-size: 14px;'>Thank you for your order!</p>
        </div>
        <div style='padding: 28px;'>
            <h2 style='font-size: 18px; margin-top: 0; color: #0f172a;'>Order Confirmation #{orderCode}</h2>
            <p style='color: #475569; font-size: 14px; line-height: 1.5;'>Hi {shippingName}, we have received your order and are currently processing it with care.</p>
            
            <table style='width: 100%; border-collapse: collapse; margin: 20px 0; font-size: 14px;'>
                <thead>
                    <tr style='background: #f8fafc; color: #64748b; font-size: 12px; text-transform: uppercase;'>
                        <th style='padding: 10px 8px; text-align: left;'>Product</th>
                        <th style='padding: 10px 8px; text-align: center;'>Qty</th>
                        <th style='padding: 10px 8px; text-align: right;'>Price</th>
                        <th style='padding: 10px 8px; text-align: right;'>Total</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan='3' style='padding: 16px 8px 8px; text-align: right; font-weight: 700; font-size: 16px;'>Grand Total:</td>
                        <td style='padding: 16px 8px 8px; text-align: right; font-weight: 800; font-size: 18px; color: #2563eb;'>${order.TotalAmount:N2}</td>
                    </tr>
                </tfoot>
            </table>

            <div style='background: #f8fafc; border-radius: 12px; padding: 16px; margin-top: 24px; font-size: 13px; color: #475569;'>
                <p style='margin: 0 0 6px 0;'><strong>Order Date:</strong> {orderDate}</p>
                <p style='margin: 0;'><strong>Order Status:</strong> {order.Status}</p>
            </div>
        </div>
        <div style='text-align: center; padding: 20px; font-size: 12px; color: #94a3b8; border-top: 1px solid #f1f5f9;'>
            &copy; {DateTime.UtcNow.Year} CLOTHÉ Store. If you have questions, please reach out to support.
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(customerEmail, $"[CLOTHÉ] Order Confirmation #{orderCode}", html);
        }

        public async Task<bool> SendAdminOrderNotificationAsync(Order order)
        {
            var settings = GetSettings();
            if (!settings.NotifyOnNewOrder || string.IsNullOrWhiteSpace(settings.AdminEmail)) return false;

            var orderCode = order.Id.ToString()[..8].ToUpper();
            var customerName = $"{order.Customer?.FirstName} {order.Customer?.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(customerName)) customerName = order.Customer?.Email ?? "Guest Customer";

            var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; background: #f1f5f9; color: #1e293b; padding: 24px;'>
    <div style='background: #ffffff; max-width: 580px; margin: 0 auto; border-radius: 14px; border: 1px solid #e2e8f0; padding: 24px;'>
        <div style='display: flex; align-items: center; justify-content: space-between; border-bottom: 2px solid #3b82f6; padding-bottom: 12px;'>
            <h2 style='margin: 0; color: #0f172a; font-size: 18px;'>New Order Notification</h2>
            <span style='background: #dbeafe; color: #1e40af; font-weight: 700; padding: 4px 10px; border-radius: 9999px; font-size: 12px;'>#{orderCode}</span>
        </div>
        <div style='margin-top: 16px; font-size: 14px; line-height: 1.6;'>
            <p>A new order has been successfully placed in the store:</p>
            <ul style='padding-left: 20px;'>
                <li><strong>Customer:</strong> {customerName} ({order.Customer?.Email})</li>
                <li><strong>Total Amount:</strong> <span style='color: #16a34a; font-weight: bold;'>${order.TotalAmount:N2}</span></li>
                <li><strong>Order Status:</strong> {order.Status}</li>
                <li><strong>Order Date:</strong> {order.OrderDate.ToLocalTime():yyyy-MM-dd HH:mm:ss}</li>
            </ul>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(settings.AdminEmail, $"[ADMIN ALERT] New Order Placed #{orderCode} - ${order.TotalAmount:N2}", html);
        }

        public async Task<bool> SendPaymentReceiptEmailAsync(Payment payment, Order order)
        {
            var settings = GetSettings();
            if (!settings.NotifyOnPaymentSuccess) return false;

            var customerEmail = order.Customer?.Email;
            if (string.IsNullOrWhiteSpace(customerEmail)) return false;

            var orderCode = order.Id.ToString()[..8].ToUpper();
            var payCode = payment.Id.ToString()[..8].ToUpper();
            var payMethod = payment.PaymentMethod?.Name ?? "Online Payment";

            var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; background: #f8fafc; color: #1e293b; padding: 24px;'>
    <div style='background: #ffffff; max-width: 580px; margin: 0 auto; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden;'>
        <div style='background: #16a34a; color: white; padding: 24px; text-align: center;'>
            <h2 style='margin: 0; font-size: 20px;'>Payment Confirmed!</h2>
            <p style='margin: 6px 0 0 0; opacity: 0.9; font-size: 14px;'>Receipt for Order #{orderCode}</p>
        </div>
        <div style='padding: 24px; font-size: 14px; line-height: 1.6;'>
            <p>Your payment has been successfully verified. Here are the transaction details:</p>
            <div style='background: #f8fafc; border-radius: 12px; padding: 16px; margin: 16px 0;'>
                <p style='margin: 0 0 8px 0;'><strong>Transaction ID:</strong> #{payCode}</p>
                <p style='margin: 0 0 8px 0;'><strong>Payment Method:</strong> {payMethod}</p>
                <p style='margin: 0 0 8px 0;'><strong>Amount Paid:</strong> <span style='color: #16a34a; font-weight: 700;'>${payment.Amount:N2}</span></p>
                <p style='margin: 0;'><strong>Status:</strong> {payment.PaymentStatus}</p>
            </div>
            <p>Thank you for shopping with CLOTHÉ!</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(customerEmail, $"[CLOTHÉ] Payment Receipt for Order #{orderCode}", html);
        }

        public async Task<bool> SendLowStockAlertEmailAsync(string productName, string variantSku, string size, string color, int currentStock)
        {
            var settings = GetSettings();
            if (!settings.NotifyOnLowStock || string.IsNullOrWhiteSpace(settings.AdminEmail)) return false;

            var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; background: #fef2f2; color: #1e293b; padding: 24px;'>
    <div style='background: #ffffff; max-width: 550px; margin: 0 auto; border-radius: 14px; border: 1px solid #fecaca; padding: 24px;'>
        <h2 style='color: #dc2626; margin-top: 0; font-size: 18px;'>⚠️ Low Stock Warning</h2>
        <p style='font-size: 14px; color: #475569;'>The inventory for the following item has fallen below the safety threshold:</p>
        <div style='background: #fff1f2; border-left: 4px solid #e11d48; padding: 12px 16px; margin: 16px 0; font-size: 14px;'>
            <p style='margin: 0 0 6px 0;'><strong>Product:</strong> {productName}</p>
            <p style='margin: 0 0 6px 0;'><strong>SKU:</strong> {variantSku}</p>
            <p style='margin: 0 0 6px 0;'><strong>Option:</strong> {size} / {color}</p>
            <p style='margin: 0;'><strong>Remaining Quantity:</strong> <span style='color: #dc2626; font-weight: 800;'>{currentStock}</span></p>
        </div>
        <p style='font-size: 13px; color: #64748b;'>Please re-order inventory promptly to avoid stockouts.</p>
    </div>
</body>
</html>";

            return await SendEmailAsync(settings.AdminEmail, $"[LOW STOCK ALERT] {productName} ({size}/{color}) - {currentStock} left", html);
        }

        public async Task<bool> SendShipmentStatusEmailAsync(Shipment shipment, Order order)
        {
            var settings = GetSettings();
            if (!settings.NotifyOnShipmentUpdate) return false;

            var customerEmail = order.Customer?.Email;
            if (string.IsNullOrWhiteSpace(customerEmail)) return false;

            var orderCode = order.Id.ToString()[..8].ToUpper();
            var tracking = string.IsNullOrWhiteSpace(shipment.TrackingNumber) ? "Pending" : shipment.TrackingNumber;

            var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; background: #f8fafc; color: #1e293b; padding: 24px;'>
    <div style='background: #ffffff; max-width: 580px; margin: 0 auto; border-radius: 16px; border: 1px solid #e2e8f0; padding: 24px;'>
        <h2 style='color: #2563eb; margin-top: 0; font-size: 20px;'>🚚 Shipment Update</h2>
        <p style='font-size: 14px; color: #475569;'>Your shipment for Order #{orderCode} status is now: <strong>{shipment.ShipmentStatus}</strong>.</p>
        <div style='background: #f1f5f9; border-radius: 10px; padding: 14px; margin: 16px 0; font-size: 14px;'>
            <p style='margin: 0 0 6px 0;'><strong>Carrier:</strong> {shipment.ShippingCompany ?? "CLOTHÉ Logistics"}</p>
            <p style='margin: 0 0 6px 0;'><strong>Tracking Code:</strong> {tracking}</p>
            <p style='margin: 0;'><strong>Status:</strong> {shipment.ShipmentStatus}</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(customerEmail, $"[CLOTHÉ] Shipment Update: {shipment.ShipmentStatus} for Order #{orderCode}", html);
        }

        public async Task<bool> SendContactInquiryCustomerConfirmationAsync(string name, string email, string? phone, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var safeName = string.IsNullOrWhiteSpace(name) ? "Valued Customer" : name.Trim();
            var nowStr = DateTime.Now.ToString("MMM dd, yyyy 'at' hh:mm tt");

            var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; background: #f8fafc; color: #1e293b; margin: 0; padding: 24px;'>
    <div style='background: #ffffff; max-width: 600px; margin: 0 auto; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);'>
        <div style='background: #0f172a; color: white; padding: 28px; text-align: center;'>
            <h1 style='margin: 0; font-size: 24px; font-weight: 800; letter-spacing: 1px;'>CLOTHÉ</h1>
            <p style='margin: 6px 0 0 0; color: #94a3b8; font-size: 13px;'>Customer Support Desk</p>
        </div>
        <div style='padding: 28px;'>
            <h2 style='font-size: 18px; margin-top: 0; color: #0f172a;'>We Received Your Message!</h2>
            <p style='color: #475569; font-size: 14px; line-height: 1.6;'>
                Hi <strong>{safeName}</strong>,<br/>
                Thank you for reaching out to us. We have received your inquiry regarding <strong>""{subject}""</strong>. Our support team is reviewing it and will respond to you shortly.
            </p>
            
            <div style='background: #f8fafc; border-radius: 12px; border: 1px solid #e2e8f0; padding: 18px; margin: 20px 0;'>
                <div style='font-size: 11px; font-weight: 700; color: #64748b; text-transform: uppercase; margin-bottom: 8px;'>Your Inquiry Summary</div>
                <p style='margin: 0 0 6px 0; font-size: 13px;'><strong>Subject:</strong> {subject}</p>
                <p style='margin: 0 0 6px 0; font-size: 13px;'><strong>Date Sent:</strong> {nowStr}</p>
                <div style='margin-top: 10px; padding: 12px; background: #ffffff; border-radius: 8px; border: 1px solid #e2e8f0; font-size: 13px; color: #334155; white-space: pre-wrap;'>{message}</div>
            </div>

            <p style='color: #475569; font-size: 13px; line-height: 1.5;'>
                Need faster assistance? You can also reach our live support bot on Telegram:
                <a href='https://t.me/khmerclothingstore_bot' style='color: #2563eb; font-weight: 600; text-decoration: none;'>@@khmerclothingstore_bot</a> or call our hotline at <strong>096 914 4183</strong>.
            </p>
        </div>
        <div style='text-align: center; padding: 20px; font-size: 12px; color: #94a3b8; border-top: 1px solid #f1f5f9;'>
            &copy; {DateTime.UtcNow.Year} CLOTHÉ Store. Preah Norodom Blvd, Phnom Penh, Cambodia.
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(email.Trim(), $"[CLOTHÉ] We Received Your Message: {subject}", html);
        }

        public async Task<bool> SendContactInquiryAdminNotificationAsync(string name, string email, string? phone, string subject, string message)
        {
            var settings = GetSettings();
            if (string.IsNullOrWhiteSpace(settings.AdminEmail)) return false;

            var safeName = string.IsNullOrWhiteSpace(name) ? "Anonymous User" : name.Trim();
            var nowStr = DateTime.Now.ToString("MMM dd, yyyy 'at' hh:mm tt");
            var phoneDisplay = string.IsNullOrWhiteSpace(phone) ? "None provided" : phone.Trim();

            var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; background: #f1f5f9; color: #1e293b; padding: 24px;'>
    <div style='background: #ffffff; max-width: 600px; margin: 0 auto; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);'>
        <div style='background: linear-gradient(135deg, #2563eb, #1d4ed8); color: white; padding: 24px;'>
            <div style='font-size: 11px; text-transform: uppercase; letter-spacing: 1px; opacity: 0.9;'>ADMIN ALERT</div>
            <h2 style='margin: 4px 0 0 0; font-size: 20px; font-weight: 800;'>New Customer Inquiry</h2>
        </div>
        <div style='padding: 24px;'>
            <div style='background: #f8fafc; border-radius: 12px; border: 1px solid #e2e8f0; padding: 16px; margin-bottom: 20px;'>
                <table style='width: 100%; font-size: 13px; line-height: 1.6;'>
                    <tr><td style='width: 100px; color: #64748b; font-weight: 600;'>From:</td><td><strong>{safeName}</strong></td></tr>
                    <tr><td style='color: #64748b; font-weight: 600;'>Email:</td><td><a href='mailto:{email}' style='color: #2563eb; text-decoration: none;'>{email}</a></td></tr>
                    <tr><td style='color: #64748b; font-weight: 600;'>Phone:</td><td><a href='tel:{phone}' style='color: #16a34a; text-decoration: none;'>{phoneDisplay}</a></td></tr>
                    <tr><td style='color: #64748b; font-weight: 600;'>Subject:</td><td><strong>{subject}</strong></td></tr>
                    <tr><td style='color: #64748b; font-weight: 600;'>Received:</td><td>{nowStr}</td></tr>
                </table>
            </div>

            <div style='margin-bottom: 20px;'>
                <div style='font-size: 12px; font-weight: 700; color: #475569; margin-bottom: 6px;'>Message Body:</div>
                <div style='padding: 16px; background: #fff1f2; border-left: 4px solid #f43f5e; border-radius: 8px; font-size: 14px; line-height: 1.6; color: #1e293b; white-space: pre-wrap;'>{message}</div>
            </div>

            <div style='text-align: center; margin-top: 24px;'>
                <a href='mailto:{email}?subject=Re:%20{Uri.EscapeDataString(subject)}' style='display: inline-block; padding: 12px 24px; background: #2563eb; color: white; border-radius: 10px; font-size: 13px; font-weight: 700; text-decoration: none;'>Reply via Email</a>
            </div>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(settings.AdminEmail, $"[INQUIRY ALERT] {subject} - From {safeName}", html);
        }
    }
}
