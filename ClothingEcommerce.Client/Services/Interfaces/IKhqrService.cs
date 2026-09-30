namespace WebApplication_ClothingEcommerce.Services
{
    public class KhqrResponse
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
        public string Reference { get; set; } = string.Empty;
        public string AbaDeepLink { get; set; } = string.Empty;
    }

    public class KhqrTransactionResult
    {
        public bool IsPaid { get; set; }
        public int ResponseCode { get; set; } = -1;
        public string ResponseMessage { get; set; } = string.Empty;
        public string? Hash { get; set; }
        public string? FromAccountId { get; set; }
        public string? ToAccountId { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? Description { get; set; }
        public long? CreatedDateMs { get; set; }
    }

    public class KhqrConfig
    {
        public string BaseUrl { get; set; } = "https://api-bakong.nbc.gov.kh";
        public string Token { get; set; } = string.Empty;
        public string BakongAccount { get; set; } = "sorn_sophiram@bkrt";
        public string MerchantName { get; set; } = "SOPHIRAM SORN";
        public string StoreLabel { get; set; } = "Multi-Vendor Marketplace";
        public string Phone { get; set; } = "0969144183";
        public string City { get; set; } = "Phnom Penh";
        public string Currency { get; set; } = "USD";
        public decimal UsdToKhrRate { get; set; } = 4100m;
    }

    public interface IKhqrService
    {
        /// <summary>
        /// Generates an authentic National Bank of Cambodia (NBC) Bakong KHQR dynamic EMVCo payload.
        /// </summary>
        KhqrResponse GenerateKhqr(decimal amount, string currency = "USD", string? reference = null);

        /// <summary>
        /// Polls or checks transaction payment status on the Bakong network by KHQR MD5 hash.
        /// </summary>
        Task<KhqrTransactionResult> CheckTransactionByMd5Async(string md5);

        /// <summary>
        /// Generates ABA Mobile deep link URI for opening ABA Mobile app on mobile devices.
        /// </summary>
        string GenerateAbaDeepLink(string khqrString);

        /// <summary>
        /// Retrieves the current KHQR & Bakong Open API configuration.
        /// </summary>
        KhqrConfig GetConfig();
    }
}
