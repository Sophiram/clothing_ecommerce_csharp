using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly AppDbContext _context;

        public ShipmentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Shipment>> GetAllShipmentsAsync(string? search = null, ShipmentStatus? status = null)
        {
            var query = _context.Shipments
                .AsNoTracking()
                .Include(s => s.Order)
                    .ThenInclude(o => o.Customer)
                .Include(s => s.Order)
                    .ThenInclude(o => o.Address)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(s =>
                    s.TrackingNumber.Contains(search) ||
                    s.ShippingCompany.Contains(search) ||
                    s.OrderId.ToString().Contains(search) ||
                    (s.Order.Customer != null && (
                        s.Order.Customer.FirstName.Contains(search) ||
                        s.Order.Customer.LastName.Contains(search) ||
                        s.Order.Customer.Email.Contains(search)
                    ))
                );
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.ShipmentStatus == status.Value);
            }

            return await query
                .OrderByDescending(s => s.Order.OrderDate)
                .ToListAsync();
        }

        public async Task<Shipment?> GetShipmentByIdAsync(Guid id)
        {
            return await _context.Shipments
                .AsNoTracking()
                .Include(s => s.Order)
                    .ThenInclude(o => o.Customer)
                .Include(s => s.Order)
                    .ThenInclude(o => o.Address)
                .Include(s => s.Order)
                    .ThenInclude(o => o.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<bool> UpdateShipmentAsync(
            Guid id,
            string trackingNumber,
            string shippingCompany,
            ShipmentStatus status,
            DateTime? shippedAt,
            DateTime? deliveredAt)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null) return false;

            shipment.TrackingNumber = trackingNumber?.Trim() ?? string.Empty;
            shipment.ShippingCompany = string.IsNullOrWhiteSpace(shippingCompany) ? "Standard Courier" : shippingCompany.Trim();
            shipment.ShipmentStatus = status;

            // Auto timestamps if not set
            if (status == ShipmentStatus.Shipped || status == ShipmentStatus.InTransit)
            {
                shipment.ShippedAt = shippedAt ?? shipment.ShippedAt ?? DateTime.UtcNow;
                if (shipment.Order != null && shipment.Order.Status != OrderStatus.Delivered && shipment.Order.Status != OrderStatus.Cancelled)
                {
                    shipment.Order.Status = OrderStatus.Shipped;
                }
            }
            else if (status == ShipmentStatus.Delivered)
            {
                shipment.ShippedAt ??= DateTime.UtcNow;
                shipment.DeliveredAt = deliveredAt ?? shipment.DeliveredAt ?? DateTime.UtcNow;
                if (shipment.Order != null)
                {
                    shipment.Order.Status = OrderStatus.Delivered;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> QuickUpdateStatusAsync(Guid id, ShipmentStatus status, string? trackingNumber = null)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null) return false;

            shipment.ShipmentStatus = status;
            if (!string.IsNullOrWhiteSpace(trackingNumber))
            {
                shipment.TrackingNumber = trackingNumber.Trim();
            }

            if (status == ShipmentStatus.Shipped || status == ShipmentStatus.InTransit)
            {
                shipment.ShippedAt ??= DateTime.UtcNow;
                if (shipment.Order != null && shipment.Order.Status != OrderStatus.Delivered && shipment.Order.Status != OrderStatus.Cancelled)
                {
                    shipment.Order.Status = OrderStatus.Shipped;
                }
            }
            else if (status == ShipmentStatus.Delivered)
            {
                shipment.ShippedAt ??= DateTime.UtcNow;
                shipment.DeliveredAt ??= DateTime.UtcNow;
                if (shipment.Order != null)
                {
                    shipment.Order.Status = OrderStatus.Delivered;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FulfillOrderAsync(Guid orderId, string carrier, string trackingNumber)
        {
            var order = await _context.Orders
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            var finalTracking = string.IsNullOrWhiteSpace(trackingNumber)
                ? $"TRK-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : trackingNumber.Trim();
            var finalCarrier = string.IsNullOrWhiteSpace(carrier)
                ? "Standard Courier"
                : carrier.Trim();

            if (order.Shipment == null)
            {
                var newShipment = new Shipment
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ShippingCompany = finalCarrier,
                    TrackingNumber = finalTracking,
                    ShipmentStatus = ShipmentStatus.Shipped,
                    ShippedAt = DateTime.UtcNow
                };
                _context.Shipments.Add(newShipment);
            }
            else
            {
                order.Shipment.ShippingCompany = finalCarrier;
                order.Shipment.TrackingNumber = finalTracking;
                order.Shipment.ShipmentStatus = ShipmentStatus.Shipped;
                order.Shipment.ShippedAt ??= DateTime.UtcNow;
            }

            if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Cancelled)
            {
                order.Status = OrderStatus.Shipped;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateShipmentAsync(Shipment shipment)
        {
            if (shipment.Id == Guid.Empty) shipment.Id = Guid.NewGuid();
            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteShipmentAsync(Guid id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null) return false;

            _context.Shipments.Remove(shipment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
