using System.ComponentModel.DataAnnotations;

namespace ClothingEcommerce.Shared.DTOs.Pos
{
    public class PosProductVariantDto
    {
        public Guid VariantId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string? ColorHex { get; set; }
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public int StockQuantity { get; set; }
        public int AvailableQuantity { get; set; }
    }

    public class PosProductDto
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public string? ImageUrl { get; set; }
        public decimal BasePrice { get; set; }
        public int TotalAvailableStock { get; set; }
        public List<PosProductVariantDto> Variants { get; set; } = new();
    }

    public class PosCartItemRequestDto
    {
        [Required]
        public Guid VariantId { get; set; }

        [Range(1, 9999, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; } = 0;
    }

    public class PosCheckoutRequestDto
    {
        [Required(ErrorMessage = "At least one item is required.")]
        public List<PosCartItemRequestDto> Items { get; set; } = new();

        public string? CustomerName { get; set; } = "Walk-in Customer";
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        public string PaymentMethod { get; set; } = "Cash"; // "Cash", "KHQR"

        public decimal DiscountAmount { get; set; } = 0;
        public decimal CashTenderedUsd { get; set; } = 0;
        public decimal CashTenderedKhr { get; set; } = 0;
        public decimal ExchangeRate { get; set; } = 4100;

        public string? Note { get; set; }
    }

    public class PosCheckoutResponseDto
    {
        public bool Success { get; set; }
        public Guid OrderId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string CashierName { get; set; } = string.Empty;

        public decimal SubtotalUsd { get; set; }
        public decimal DiscountUsd { get; set; }
        public decimal TotalAmountUsd { get; set; }
        public decimal TotalAmountKhr { get; set; }
        public decimal ExchangeRate { get; set; } = 4100;

        public string PaymentMethod { get; set; } = "Cash";
        public string PaymentStatus { get; set; } = "Paid";

        public decimal CashTenderedUsd { get; set; }
        public decimal CashTenderedKhr { get; set; }
        public decimal ChangeUsd { get; set; }
        public decimal ChangeKhr { get; set; }

        // Dynamic KHQR
        public string? KhqrPayload { get; set; }
        public string? KhqrMd5 { get; set; }

        public List<PosReceiptItemDto> Items { get; set; } = new();
    }

    public class PosReceiptItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string VariantSku { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class PosDailySummaryDto
    {
        public DateTime Date { get; set; }
        public string CashierId { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public int TotalTransactions { get; set; }
        public decimal TotalSalesUsd { get; set; }
        public decimal TotalSalesKhr { get; set; }
        public decimal CashSalesUsd { get; set; }
        public decimal KhqrSalesUsd { get; set; }
        public decimal TotalItemsSold { get; set; }
        public decimal ExchangeRate { get; set; } = 4100;
    }
}
