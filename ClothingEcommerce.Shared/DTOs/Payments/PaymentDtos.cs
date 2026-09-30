namespace ClothingEcommerce.Shared.DTOs.Payments
{
    public class KhqrGenerateRequestDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? OrderId { get; set; }
        public string? Reference { get; set; }
    }

    public class KhqrGenerateResponseDto
    {
        public string QrString { get; set; } = string.Empty;
        public string Md5 { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal KhrAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public string MerchantName { get; set; } = string.Empty;
        public string StoreLabel { get; set; } = string.Empty;
        public string MerchantCity { get; set; } = string.Empty;
        public string BakongId { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? AbaDeepLink { get; set; }
    }

    public class PaymentStatusResponseDto
    {
        public bool Paid { get; set; }
        public string Status { get; set; } = "PENDING";
        public int ResponseCode { get; set; }
        public string? Message { get; set; }
        public string? Hash { get; set; }
        public string? FromAccountId { get; set; }
        public string? ToAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public long? CreatedDateMs { get; set; }
    }
}
