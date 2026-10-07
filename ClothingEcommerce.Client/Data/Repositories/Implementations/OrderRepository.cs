using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Implementations
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Order?> GetOrderWithDetailsAsync(Guid orderId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.Payment)
                    .ThenInclude(p => p != null ? p.PaymentMethod : null)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByCustomerIdAsync(Guid customerId)
        {
            return await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.Payment)
                    .ThenInclude(p => p != null ? p.PaymentMethod : null)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetAllOrdersWithDetailsAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Payment)
                    .ThenInclude(p => p != null ? p.PaymentMethod : null)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Payment)
                    .ThenInclude(p => p != null ? p.PaymentMethod : null)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByOrderIdAsync(Guid orderId)
        {
            return await _context.Payments
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }
    }
}
