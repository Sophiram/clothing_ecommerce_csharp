using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IShipmentService
    {
        Task<List<Shipment>> GetAllShipmentsAsync(string? search = null, ShipmentStatus? status = null);
        Task<Shipment?> GetShipmentByIdAsync(Guid id);
        Task<bool> UpdateShipmentAsync(Guid id, string trackingNumber, string shippingCompany, ShipmentStatus status, DateTime? shippedAt, DateTime? deliveredAt);
        Task<bool> QuickUpdateStatusAsync(Guid id, ShipmentStatus status, string? trackingNumber = null);
        Task<bool> FulfillOrderAsync(Guid orderId, string carrier, string trackingNumber);
        Task<bool> CreateShipmentAsync(Shipment shipment);
        Task<bool> DeleteShipmentAsync(Guid id);
    }
}
