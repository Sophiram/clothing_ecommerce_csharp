namespace WebApplication_ClothingEcommerce.Models
{
    public class StockMovement
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductVariantId { get; set; }

        public string MovementType { get; set; } = "StockIn"; // StockIn, StockOut, Damaged, Return, AuditCorrection, PosSale

        public int Quantity { get; set; }

        public int PreviousQuantity { get; set; }

        public int NewQuantity { get; set; }

        public string? Reason { get; set; }

        public string? Reference { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ProductVariant ProductVariant { get; set; } = null!;
    }
}
