using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PAYMENT METHODS
        // =========================================================

        public async Task<List<PaymentMethod>> GetActivePaymentMethodsAsync()
        {
            return await _context.PaymentMethods
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<List<PaymentMethod>> GetAllPaymentMethodsAsync(string? search = null)
        {
            var query = _context.PaymentMethods.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(x => x.Name.Contains(s) || (x.Description != null && x.Description.Contains(s)));
            }

            return await query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<PaymentMethod?> GetPaymentMethodByIdAsync(Guid id)
        {
            return await _context.PaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ServiceResult> CreatePaymentMethodAsync(PaymentMethod method)
        {
            var nameExists = await _context.PaymentMethods
                .AnyAsync(m => m.Name.ToLower() == method.Name.ToLower());

            if (nameExists)
            {
                return ServiceResult.Fail("A payment method with this name already exists.");
            }

            method.Id = Guid.NewGuid();
            _context.PaymentMethods.Add(method);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Payment method created successfully.");
        }

        public async Task<ServiceResult> UpdatePaymentMethodAsync(PaymentMethod method)
        {
            var existing = await _context.PaymentMethods.FirstOrDefaultAsync(m => m.Id == method.Id);
            if (existing == null) return ServiceResult.Fail("Payment method not found.");

            var nameExists = await _context.PaymentMethods
                .AnyAsync(m => m.Id != method.Id && m.Name.ToLower() == method.Name.ToLower());

            if (nameExists)
            {
                return ServiceResult.Fail("Another payment method with this name already exists.");
            }

            existing.Name = method.Name;
            existing.Description = method.Description;
            existing.Icon = method.Icon;
            existing.IsActive = method.IsActive;
            existing.DisplayOrder = method.DisplayOrder;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Payment method updated successfully.");
        }

        public async Task<ServiceResult> DeletePaymentMethodAsync(Guid id)
        {
            var method = await _context.PaymentMethods.FirstOrDefaultAsync(m => m.Id == id);
            if (method == null) return ServiceResult.Fail("Payment method not found.");

            var hasPayments = await _context.Payments.AnyAsync(p => p.PaymentMethodId == id);
            if (hasPayments)
            {
                return ServiceResult.Fail("Cannot delete this payment method because transactions are associated with it. Disable it instead.");
            }

            _context.PaymentMethods.Remove(method);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Payment method deleted successfully.");
        }

        public async Task<ServiceResult> TogglePaymentMethodStatusAsync(Guid id)
        {
            var method = await _context.PaymentMethods.FirstOrDefaultAsync(m => m.Id == id);
            if (method == null) return ServiceResult.Fail("Payment method not found.");

            method.IsActive = !method.IsActive;
            await _context.SaveChangesAsync();

            var status = method.IsActive ? "activated" : "deactivated";
            return ServiceResult.Ok($"Payment method {status} successfully.");
        }

        // =========================================================
        // PAYMENTS
        // =========================================================

        public async Task<(List<Payment> Payments, PaymentStatsDto Stats)> GetPaymentsAsync(string? search, string? status, Guid? paymentMethodId)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                .Include(p => p.PaymentMethod)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p => p.OrderId.ToString().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(p => p.PaymentStatus == parsedStatus);
            }

            if (paymentMethodId.HasValue && paymentMethodId.Value != Guid.Empty)
            {
                query = query.Where(p => p.PaymentMethodId == paymentMethodId.Value);
            }

            var payments = await query.OrderByDescending(p => p.PaidAt ?? DateTime.MinValue).ToListAsync();

            var allPayments = await _context.Payments.AsNoTracking().ToListAsync();
            var totalRevenue = allPayments
                .Where(p => p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed)
                .Sum(p => p.Amount);
            var paidCount = allPayments.Count(p => p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed);
            var pendingCount = allPayments.Count(p => p.PaymentStatus == PaymentStatus.Pending);
            var failedCount = allPayments.Count(p => p.PaymentStatus == PaymentStatus.Failed || p.PaymentStatus == PaymentStatus.Cancelled || p.PaymentStatus == PaymentStatus.Refunded);

            var stats = new PaymentStatsDto
            {
                TotalRevenue = totalRevenue,
                PaidCount = paidCount,
                PendingCount = pendingCount,
                FailedCount = failedCount
            };

            return (payments, stats);
        }

        public async Task<Payment?> GetPaymentByIdAsync(Guid id)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.PaymentMethod)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ServiceResult> CreatePaymentAsync(Payment payment)
        {
            var existingForOrder = await _context.Payments.AnyAsync(p => p.OrderId == payment.OrderId);
            if (existingForOrder)
            {
                return ServiceResult.Fail("A payment record already exists for this order.");
            }

            payment.Id = Guid.NewGuid();
            if (payment.PaymentStatus == PaymentStatus.Paid || payment.PaymentStatus == PaymentStatus.Completed)
            {
                payment.PaidAt ??= DateTime.UtcNow;
            }

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Payment recorded successfully.");
        }

        public async Task<ServiceResult> UpdatePaymentAsync(Payment payment)
        {
            var existing = await _context.Payments.FirstOrDefaultAsync(p => p.Id == payment.Id);
            if (existing == null) return ServiceResult.Fail("Payment record not found.");

            existing.PaymentMethodId = payment.PaymentMethodId;
            existing.PaymentStatus = payment.PaymentStatus;
            existing.Amount = payment.Amount;

            if ((payment.PaymentStatus == PaymentStatus.Paid || payment.PaymentStatus == PaymentStatus.Completed) && existing.PaidAt == null)
            {
                existing.PaidAt = DateTime.UtcNow;
            }
            else if (payment.PaymentStatus == PaymentStatus.Pending || payment.PaymentStatus == PaymentStatus.Failed)
            {
                existing.PaidAt = null;
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Payment updated successfully.");
        }

        public async Task<ServiceResult> DeletePaymentAsync(Guid id)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null) return ServiceResult.Fail("Payment record not found.");

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Payment deleted successfully.");
        }

        public async Task<(List<Order> OrdersWithoutPayment, List<PaymentMethod> ActiveMethods)> GetPaymentCreateSelectListsAsync()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => !_context.Payments.Any(p => p.OrderId == o.Id))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var methods = await GetActivePaymentMethodsAsync();
            return (orders, methods);
        }

        public async Task<List<PaymentMethod>> GetPaymentMethodsSelectListAsync()
        {
            return await _context.PaymentMethods
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}
