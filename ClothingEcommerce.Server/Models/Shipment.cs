using WebApplication_ClothingEcommerce.Data.Enums;

namespace WebApplication_ClothingEcommerce.Models
{
    public class Shipment
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public string TrackingNumber { get; set; } = string.Empty;

        public string ShippingCompany { get; set; } = string.Empty;

        public ShipmentStatus ShipmentStatus { get; set; }

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public Order Order { get; set; } = null!;
    }
}
