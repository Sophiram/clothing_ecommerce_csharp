using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Services
{
    public class BakongPaymentService : IBakongPaymentService
    {
        private readonly AppDbContext _context;
        private readonly IKhqrService _khqrService;
        private readonly ICartService _cartService;
        private readonly ITelegramService _telegramService;
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BakongPaymentService> _logger;

        public BakongPaymentService(
            AppDbContext context,
            IKhqrService khqrService,
            ICartService cartService,
            ITelegramService telegramService,
            IEmailService emailService,
            IServiceScopeFactory scopeFactory,
            ILogger<BakongPaymentService> logger)
        {
            _context = context;
            _khqrService = khqrService;
            _cartService = cartService;
            _telegramService = telegramService;
            _emailService = emailService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<BakongPaymentResponse> CreatePaymentAttemptAsync(Guid orderId, string currency = "USD", string? userId = null)
        {
            if (orderId == Guid.Empty)
            {
                return new BakongPaymentResponse { Success = false, Message = "Order ID is required." };
            }

            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new BakongPaymentResponse { Success = false, Message = "Order was not found." };
            }

            // Security: If userId provided, ensure customer ownership
            if (!string.IsNullOrEmpty(userId) && order.Customer != null && order.Customer.ApplicationUserId != userId)
            {
                return new BakongPaymentResponse { Success = false, Message = "Unauthorized access to order." };
            }

            // Security: Never trust client amount. Calculate from database.
            var orderTotal = order.TotalAmount;
            if (orderTotal <= 0)
            {
                return new BakongPaymentResponse { Success = false, Message = "Invalid order amount." };
            }

            // Idempotency: Prevent double payment if already Paid
            if (order.Payment != null && order.Payment.PaymentStatus == PaymentStatus.Paid)
            {
                return new BakongPaymentResponse
                {
                    Success = true,
                    PaymentId = order.Payment.Id,
                    OrderId = order.Id,
                    OrderNumber = "#" + order.Id.ToString()[..8].ToUpper(),
                    Amount = order.TotalAmount,
                    Currency = order.Payment.Currency,
                    Status = "PAID",
                    Message = "This order has already been paid."
                };
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return new BakongPaymentResponse { Success = false, Message = "This order has been cancelled and cannot be paid." };
            }

            // Locate active Bakong/KHQR payment method
            PaymentMethod? paymentMethod = null;
            if (order.Payment != null && order.Payment.PaymentMethodId != Guid.Empty)
            {
                paymentMethod = await _context.PaymentMethods
                    .FirstOrDefaultAsync(m => m.Id == order.Payment.PaymentMethodId && m.IsActive);
            }

            if (paymentMethod == null)
            {
                var activeMethods = await _context.PaymentMethods
                    .AsNoTracking()
                    .Where(m => m.IsActive)
                    .ToListAsync();

                paymentMethod = activeMethods.FirstOrDefault(m =>
                    m.Name.Contains("KHQR", StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Contains("Bakong", StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Contains("ABA", StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Contains("ACLEDA", StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Contains("Wing", StringComparison.OrdinalIgnoreCase))
                    ?? activeMethods.FirstOrDefault();

                if (paymentMethod == null)
                {
                    return new BakongPaymentResponse { Success = false, Message = "No active payment method available." };
                }
            }

            // Create unique transaction reference for this attempt
            var orderShort = order.Id.ToString()[..8].ToUpper();
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..6].ToUpper();
            var reference = $"BK-{orderShort}-{uniqueSuffix}";

            // Generate official dynamic EMVCo KHQR payload
            var khqr = _khqrService.GenerateKhqr(orderTotal, currency, reference);
            var expiresAt = DateTime.UtcNow.AddMinutes(5);

            Payment payment;
            if (order.Payment == null)
            {
                payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PaymentMethodId = paymentMethod.Id,
                    PaymentStatus = PaymentStatus.Pending,
                    Amount = orderTotal,
                    Currency = currency,
                    BakongReference = reference,
                    QRCode = khqr.QrString,
                    Md5Hash = khqr.Md5,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };
                _context.Payments.Add(payment);
            }
            else
            {
                payment = order.Payment;
                payment.PaymentMethodId = paymentMethod.Id;
                payment.PaymentStatus = PaymentStatus.Pending;
                payment.Amount = orderTotal;
                payment.Currency = currency;
                payment.BakongReference = reference;
                payment.QRCode = khqr.QrString;
                payment.Md5Hash = khqr.Md5;
                payment.CreatedAt = DateTime.UtcNow;
                payment.ExpiresAt = expiresAt;
                payment.FailureReason = null;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Created dynamic Bakong KHQR payment attempt {PaymentId} for Order {OrderId} (Amount: {Amount} {Currency}, Ref: {Reference})",
                payment.Id, order.Id, orderTotal, currency, reference);

            return new BakongPaymentResponse
            {
                Success = true,
                PaymentId = payment.Id,
                OrderId = order.Id,
                OrderNumber = "#" + orderShort,
                QrCode = khqr.QrString,
                Amount = khqr.Amount,
                KhrAmount = khqr.KhrAmount,
                Currency = khqr.Currency,
                Reference = reference,
                Md5 = khqr.Md5,
                MerchantName = khqr.MerchantName,
                BakongAccountId = khqr.BakongId,
                AbaDeepLink = khqr.AbaDeepLink,
                ExpiresAt = expiresAt,
                ExpiresInSeconds = 300,
                Status = "PENDING"
            };
        }

        public async Task<BakongStatusResponse> GetPaymentStatusAsync(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
            {
                return new BakongStatusResponse { Success = false, Status = "FAILED", Message = "Payment ID is required." };
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                return new BakongStatusResponse { Success = false, Status = "FAILED", Message = "Payment attempt was not found." };
            }

            var orderShort = payment.OrderId.ToString()[..8].ToUpper();

            // 1. If already Paid, return immediately (Idempotency)
            if (payment.PaymentStatus == PaymentStatus.Paid)
            {
                return new BakongStatusResponse
                {
                    Success = true,
                    Status = "PAID",
                    IsPaid = true,
                    PaymentId = payment.Id,
                    OrderId = payment.OrderId,
                    OrderNumber = "#" + orderShort,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    TransactionId = payment.BakongTransactionId,
                    Reference = payment.BakongReference,
                    PaidAt = payment.PaidAt,
                    ExpiresInSeconds = 0,
                    Message = "Payment confirmed successfully."
                };
            }

            // 2. If Cancelled
            if (payment.PaymentStatus == PaymentStatus.Cancelled)
            {
                return new BakongStatusResponse
                {
                    Success = true,
                    Status = "CANCELLED",
                    IsPaid = false,
                    PaymentId = payment.Id,
                    OrderId = payment.OrderId,
                    OrderNumber = "#" + orderShort,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Reference = payment.BakongReference,
                    ExpiresInSeconds = 0,
                    Message = "Payment attempt was cancelled."
                };
            }

            // 3. Expiration Check
            if (payment.ExpiresAt.HasValue && DateTime.UtcNow >= payment.ExpiresAt.Value)
            {
                if (payment.PaymentStatus == PaymentStatus.Pending)
                {
                    payment.PaymentStatus = PaymentStatus.Expired;
                    payment.FailureReason = "Payment window expired.";
                    await _context.SaveChangesAsync();
                }

                return new BakongStatusResponse
                {
                    Success = true,
                    Status = "EXPIRED",
                    IsPaid = false,
                    PaymentId = payment.Id,
                    OrderId = payment.OrderId,
                    OrderNumber = "#" + orderShort,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Reference = payment.BakongReference,
                    ExpiresInSeconds = 0,
                    Message = "Payment request has expired. Please generate a new QR."
                };
            }

            var remainingSeconds = payment.ExpiresAt.HasValue
                ? Math.Max(0, (int)(payment.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds)
                : 0;

            // 4. Verification Check with Bakong Open API
            if (!string.IsNullOrWhiteSpace(payment.Md5Hash))
            {
                try
                {
                    var checkResult = await _khqrService.CheckTransactionByMd5Async(payment.Md5Hash);
                    if (checkResult.IsPaid)
                    {
                        // Atomic transition to Paid
                        payment.PaymentStatus = PaymentStatus.Paid;
                        payment.PaidAt = DateTime.UtcNow;
                        payment.BakongTransactionId = checkResult.Hash ?? $"BK-{Guid.NewGuid():N}";
                        payment.FailureReason = null;

                        if (payment.Order != null)
                        {
                            payment.Order.Status = OrderStatus.Processing;
                            if (payment.Order.CustomerId != Guid.Empty)
                            {
                                await _cartService.ClearCartAsync(payment.Order.CustomerId);
                            }
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Bakong KHQR payment verified for Payment {PaymentId}, Order {OrderId} (Hash: {Hash})",
                            payment.Id, payment.OrderId, payment.BakongTransactionId);

                        // Trigger notifications asynchronously with dedicated service scope
                        var confirmedPaymentId = payment.Id;
                        var confirmedOrderId = payment.OrderId;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var scopedTelegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();
                                var scopedEmail = scope.ServiceProvider.GetRequiredService<IEmailService>();

                                try
                                {
                                    await scopedTelegram.SendPaymentNotificationAsync(confirmedPaymentId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to send Telegram payment notification for {PaymentId}", confirmedPaymentId);
                                }

                                try
                                {
                                    await scopedEmail.SendPaymentReceiptByIdAsync(confirmedPaymentId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to send receipt email for payment {PaymentId}", confirmedPaymentId);
                                }

                                try
                                {
                                    await scopedEmail.SendOrderConfirmationByIdAsync(confirmedOrderId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to send email confirmation for order {OrderId}", confirmedOrderId);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed in background notification scope for payment {PaymentId}", confirmedPaymentId);
                            }
                        });

                        return new BakongStatusResponse
                        {
                            Success = true,
                            Status = "PAID",
                            IsPaid = true,
                            PaymentId = payment.Id,
                            OrderId = payment.OrderId,
                            OrderNumber = "#" + orderShort,
                            Amount = payment.Amount,
                            Currency = payment.Currency,
                            TransactionId = payment.BakongTransactionId,
                            Reference = payment.BakongReference,
                            PaidAt = payment.PaidAt,
                            ExpiresInSeconds = 0,
                            Message = "Payment verified by Bakong Open API."
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking Bakong status for payment {PaymentId}", payment.Id);
                }
            }

            return new BakongStatusResponse
            {
                Success = true,
                Status = "PENDING",
                IsPaid = false,
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                OrderNumber = "#" + orderShort,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Reference = payment.BakongReference,
                ExpiresInSeconds = remainingSeconds,
                Message = "Waiting for customer payment scan…"
            };
        }

        public async Task<BakongStatusResponse> CancelPaymentAsync(Guid paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                return new BakongStatusResponse { Success = false, Status = "FAILED", Message = "Payment attempt was not found." };
            }

            if (payment.PaymentStatus == PaymentStatus.Pending)
            {
                payment.PaymentStatus = PaymentStatus.Cancelled;
                payment.FailureReason = "Cancelled by user.";
                await _context.SaveChangesAsync();
            }

            return new BakongStatusResponse
            {
                Success = true,
                Status = "CANCELLED",
                IsPaid = false,
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Message = "Payment attempt cancelled."
            };
        }

        public async Task<BakongStatusResponse> ConfirmSimulatedPaymentAsync(Guid paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                return new BakongStatusResponse { Success = false, Status = "FAILED", Message = "Payment attempt was not found." };
            }

            if (payment.PaymentStatus == PaymentStatus.Paid)
            {
                return await GetPaymentStatusAsync(paymentId);
            }

            payment.PaymentStatus = PaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;
            payment.BakongTransactionId = "SIM-" + Guid.NewGuid().ToString("N")[..16];
            payment.FailureReason = null;

            if (payment.Order != null)
            {
                payment.Order.Status = OrderStatus.Processing;
                if (payment.Order.CustomerId != Guid.Empty)
                {
                    await _cartService.ClearCartAsync(payment.Order.CustomerId);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Payment {PaymentId} confirmed via simulation for Order {OrderId}", payment.Id, payment.OrderId);

            // Trigger notifications asynchronously with dedicated service scope
            var simPaymentId = payment.Id;
            var simOrderId = payment.OrderId;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedTelegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();
                    var scopedEmail = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    try
                    {
                        await scopedTelegram.SendPaymentNotificationAsync(simPaymentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send Telegram simulation payment notification for {PaymentId}", simPaymentId);
                    }

                    try
                    {
                        await scopedEmail.SendPaymentReceiptByIdAsync(simPaymentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send simulation receipt email for payment {PaymentId}", simPaymentId);
                    }

                    try
                    {
                        await scopedEmail.SendOrderConfirmationByIdAsync(simOrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send simulation email confirmation for order {OrderId}", simOrderId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed in background simulation notification scope for payment {PaymentId}", simPaymentId);
                }
            });

            return new BakongStatusResponse
            {
                Success = true,
                Status = "PAID",
                IsPaid = true,
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                OrderNumber = "#" + payment.OrderId.ToString()[..8].ToUpper(),
                Amount = payment.Amount,
                Currency = payment.Currency,
                TransactionId = payment.BakongTransactionId,
                Reference = payment.BakongReference,
                PaidAt = payment.PaidAt,
                ExpiresInSeconds = 0,
                Message = "Simulated payment verified."
            };
        }
    }
}
