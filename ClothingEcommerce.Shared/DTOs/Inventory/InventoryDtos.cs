using System.ComponentModel.DataAnnotations;

namespace ClothingEcommerce.Shared.DTOs.Inventory
{
    public class StockAdjustmentRequestDto
    {
        [Required(ErrorMessage = "Product variant is required.")]
        public Guid ProductVariantId { get; set; }

        [Required(ErrorMessage = "Adjustment type is required.")]
        public string MovementType { get; set; } = "StockIn"; // StockIn, StockOut, Damaged, Return, AuditCorrection, PosSale

        [Range(1, 100000, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }

        [MaxLength(255)]
        public string? Reason { get; set; }

        [MaxLength(100)]
        public string? Reference { get; set; } // PO Number, RMA number, etc.
    }

    public class StockMovementDto
    {
        public Guid Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string VariantSku { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public string? Reason { get; set; }
        public string? Reference { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StockCheckItemDto
    {
        public Guid ProductVariantId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string VariantSku { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string? ColorHex { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class StockFilterDto
    {
        public string? SearchTerm { get; set; }
        public Guid? CategoryId { get; set; }
        public bool? LowStockOnly { get; set; }
        public int LowStockThreshold { get; set; } = 5;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
