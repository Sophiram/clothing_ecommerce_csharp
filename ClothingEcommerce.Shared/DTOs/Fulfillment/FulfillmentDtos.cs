using System.ComponentModel.DataAnnotations;

namespace ClothingEcommerce.Shared.DTOs.Fulfillment
{
    public class FulfillmentItemDto
    {
        public Guid OrderItemId { get; set; }
        public Guid VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string VariantSku { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ProductImageUrl { get; set; }
    }

    public class FulfillmentOrderDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "KHQR";
        public string? CarrierName { get; set; }
        public string? TrackingNumber { get; set; }
        public int TotalItems { get; set; }
        public List<FulfillmentItemDto> Items { get; set; } = new();
    }

    public class UpdateFulfillmentStatusDto
    {
        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public string NewStatus { get; set; } = string.Empty; // "Processing" (To Pack), "Shipped" (Packed / In Transit), "Delivered", "Cancelled"

        public string? CarrierName { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Note { get; set; }
    }
}
