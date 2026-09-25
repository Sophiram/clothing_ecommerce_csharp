using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WebApplication_ClothingEcommerce.Services
{
    /// <summary>
    /// Implements National Bank of Cambodia (NBC) Bakong KHQR EMVCo generation
    /// and live transaction verification using the Bakong Open API.
    /// </summary>
    public class KhqrService : IKhqrService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<KhqrService> _logger;

        public KhqrService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<KhqrService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // ─── EMVCo / Bakong Tag Constants ──────────────────────────────
        private const string TAG_PAYLOAD_FORMAT   = "00";
        private const string TAG_POI_METHOD       = "01";
        private const string TAG_INDIVIDUAL_ACC   = "29"; // Tag 29 for Individual / Solo Merchant
        private const string TAG_MCC              = "52";
        private const string TAG_CURRENCY         = "53";
        private const string TAG_AMOUNT           = "54";
        private const string TAG_COUNTRY          = "58";
        private const string TAG_MERCHANT_NAME    = "59";
        private const string TAG_MERCHANT_CITY    = "60";
        private const string TAG_ADDITIONAL_DATA  = "62";
        private const string TAG_CRC              = "63";

        // Subtags for Tag 29 (Individual) / Tag 30 (Corporate)
        private const string SUBTAG_BAKONG_ID     = "00";
        private const string SUBTAG_ACCOUNT_INFO  = "01";
        private const string SUBTAG_ACQUIRING_BANK= "02";

        // Subtags for Tag 62 (Additional Data)
        private const string SUBTAG_BILL_NUMBER   = "01";
        private const string SUBTAG_MOBILE_NUMBER = "02";
        private const string SUBTAG_STORE_LABEL   = "03";
        private const string SUBTAG_REF_LABEL     = "05";

        private const string USD_CODE             = "840";
        private const string KHR_CODE             = "116";
        private const string KH_COUNTRY           = "KH";
        private const string DEFAULT_MCC          = "5999"; // Standard merchant category code for KHQR

        // ─────────────────────────────────────────────────────────────
        // Generate KHQR EMVCo Payload
        // ─────────────────────────────────────────────────────────────
        public KhqrResponse GenerateKhqr(decimal amount, string currency = "USD", string? reference = null)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
            }

            var config = GetConfig();
            var isKhr = string.Equals(currency, "KHR", StringComparison.OrdinalIgnoreCase);
            var currencyCode = isKhr ? KHR_CODE : USD_CODE;

            // Compute amounts
            var usdAmount = isKhr ? Math.Round(amount / config.UsdToKhrRate, 2) : amount;
            var khrAmount = isKhr ? amount : Math.Round(amount * config.UsdToKhrRate);

            // Format amount per EMVCo spec (USD = 2 decimals, KHR = whole number)
            var formattedAmount = isKhr
                ? ((long)khrAmount).ToString()
                : TrimTrailingZeros(usdAmount);

            // 1. Tag 29: Merchant Account (Bakong Individual / Solo)
            var tag29Sb = new StringBuilder();
            tag29Sb.Append(BuildTLV(SUBTAG_BAKONG_ID, config.BakongAccount.Trim()));
            var tag29 = BuildTLV(TAG_INDIVIDUAL_ACC, tag29Sb.ToString());

            // 2. Build EMVCo TLV sequence
            var sb = new StringBuilder();
            sb.Append(BuildTLV(TAG_PAYLOAD_FORMAT, "01"));    // 000201
            sb.Append(BuildTLV(TAG_POI_METHOD, "12"));        // 010212 (Dynamic QR with embedded amount)
            sb.Append(tag29);                                 // 29...
            sb.Append(BuildTLV(TAG_MCC, DEFAULT_MCC));        // 52045999 (Standard default MCC)
            sb.Append(BuildTLV(TAG_CURRENCY, currencyCode));  // 5303840 (or 116)
            sb.Append(BuildTLV(TAG_AMOUNT, formattedAmount)); // 54...
            sb.Append(BuildTLV(TAG_COUNTRY, KH_COUNTRY));     // 5802KH

            // Clean merchant name (max 25 chars)
            var cleanMerchant = CleanAscii(config.MerchantName, 25);
            sb.Append(BuildTLV(TAG_MERCHANT_NAME, cleanMerchant));

            // Clean city (max 15 chars)
            var cleanCity = CleanAscii(config.City, 15);
            sb.Append(BuildTLV(TAG_MERCHANT_CITY, cleanCity));

            // Tag 62: Additional Data (Bill Number, Mobile, Store Label)
            var refVal = reference ?? string.Empty;
            var sub62Sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(refVal))
            {
                sub62Sb.Append(BuildTLV(SUBTAG_BILL_NUMBER, CleanAscii(refVal, 25)));
            }
            if (!string.IsNullOrWhiteSpace(config.Phone))
            {
                sub62Sb.Append(BuildTLV(SUBTAG_MOBILE_NUMBER, CleanAscii(config.Phone, 25)));
            }
            if (!string.IsNullOrWhiteSpace(config.StoreLabel))
            {
                sub62Sb.Append(BuildTLV(SUBTAG_STORE_LABEL, CleanAscii(config.StoreLabel, 25)));
            }

            if (sub62Sb.Length > 0)
            {
                sb.Append(BuildTLV(TAG_ADDITIONAL_DATA, sub62Sb.ToString()));
            }

            // Tag 99: Dynamic KHQR Timestamp (Mandatory per NBC Bakong KHQR Specification)
            sb.Append(BuildTag99());

            // Tag 63: CRC placeholder
            sb.Append(TAG_CRC + "04");
            var rawData = sb.ToString();

            // Calculate CRC-16/CCITT-FALSE
            var crc = ComputeCrc16Ccitt(rawData);
            var finalKhqr = rawData + crc;

            // Generate MD5 hash of the KHQR string
            var md5Hash = ComputeMd5(finalKhqr);

            // ABA Mobile app deep link
            var abaDeepLink = GenerateAbaDeepLink(finalKhqr);

            return new KhqrResponse
            {
                QrString = finalKhqr,
                Md5 = md5Hash,
                Amount = usdAmount,
                KhrAmount = khrAmount,
                Currency = currency.ToUpper(),
                MerchantName = cleanMerchant,
                StoreLabel = config.StoreLabel,
                MerchantCity = cleanCity,
                BakongId = config.BakongAccount,
                Phone = config.Phone,
                Reference = refVal,
                AbaDeepLink = abaDeepLink
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Check Transaction Status via Bakong Open API
        // ─────────────────────────────────────────────────────────────
        public async Task<KhqrTransactionResult> CheckTransactionByMd5Async(string md5)
        {
            if (string.IsNullOrWhiteSpace(md5))
            {
                return new KhqrTransactionResult
                {
                    IsPaid = false,
                    ResponseCode = -1,
                    ResponseMessage = "MD5 hash is required."
                };
            }

            var config = GetConfig();
            if (string.IsNullOrWhiteSpace(config.Token))
            {
                return new KhqrTransactionResult
                {
                    IsPaid = false,
                    ResponseCode = -1,
                    ResponseMessage = "Bakong Open API token is not configured in .env."
                };
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BakongApi");
                using var request = new HttpRequestMessage(HttpMethod.Post, "v1/check_transaction_by_md5");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new { md5 = md5.Trim().ToLowerInvariant() }),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.SendAsync(request);
                var jsonContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(jsonContent))
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    var root = doc.RootElement;

                    var responseCode = root.TryGetProperty("responseCode", out var codeProp) ? codeProp.GetInt32() : -1;
                    var responseMessage = root.TryGetProperty("responseMessage", out var msgProp) ? msgProp.GetString() ?? string.Empty : string.Empty;

                    if (responseCode == 0)
                    {
                        var result = new KhqrTransactionResult
                        {
                            IsPaid = true,
                            ResponseCode = 0,
                            ResponseMessage = responseMessage
                        };

                        if (root.TryGetProperty("data", out var dataEl))
                        {
                            if (dataEl.TryGetProperty("hash", out var h)) result.Hash = h.GetString();
                            if (dataEl.TryGetProperty("fromAccountId", out var from)) result.FromAccountId = from.GetString();
                            if (dataEl.TryGetProperty("toAccountId", out var to)) result.ToAccountId = to.GetString();
                            if (dataEl.TryGetProperty("amount", out var a)) result.Amount = a.GetDecimal();
                            if (dataEl.TryGetProperty("currency", out var c)) result.Currency = c.GetString();
                            if (dataEl.TryGetProperty("description", out var d)) result.Description = d.GetString();
                            if (dataEl.TryGetProperty("createdDateMs", out var date)) result.CreatedDateMs = date.GetInt64();
                        }

                        return result;
                    }

                    return new KhqrTransactionResult
                    {
                        IsPaid = false,
                        ResponseCode = responseCode,
                        ResponseMessage = responseMessage
                    };
                }

                return new KhqrTransactionResult
                {
                    IsPaid = false,
                    ResponseCode = (int)response.StatusCode,
                    ResponseMessage = "Transaction pending or not found."
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bakong Open API status check failed for MD5: {Md5}", md5);
                return new KhqrTransactionResult
                {
                    IsPaid = false,
                    ResponseCode = -1,
                    ResponseMessage = "Connection error communicating with Bakong Open API."
                };
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Deep Link Generator
        // ─────────────────────────────────────────────────────────────
        public string GenerateAbaDeepLink(string khqrString)
        {
            if (string.IsNullOrWhiteSpace(khqrString)) return string.Empty;
            return $"aba://pay?khqr={Uri.EscapeDataString(khqrString)}";
        }

        // ─────────────────────────────────────────────────────────────
        // Configuration Loader
        // ─────────────────────────────────────────────────────────────
        public KhqrConfig GetConfig()
        {
            var baseUrl = _configuration["BAKONG_BASE_URL"]
                       ?? _configuration["KHQR_BASE_URL"]
                       ?? _configuration["KHQR:BaseUrl"]
                       ?? "https://api-bakong.nbc.gov.kh";

            var token = _configuration["BAKONG_TOKEN"]?.Trim('"')
                     ?? _configuration["KHQR_TOKEN"]?.Trim('"')
                     ?? _configuration["KHQR_BAKONG_TOKEN"]?.Trim('"')
                     ?? _configuration["KHQR:Token"]?.Trim('"')
                     ?? string.Empty;

            var bakongAccount = _configuration["BAKONG_ACCOUNT_ID"]
                             ?? _configuration["KHQR_BAKONG_ACCOUNT_ID"]
                             ?? _configuration["KHQR_ACCOUNT"]
                             ?? _configuration["KHQR:BakongAccountId"]
                             ?? "sorn_sophiram@bkrt";

            var merchantName = _configuration["BAKONG_MERCHANT_NAME"]
                            ?? _configuration["KHQR_MERCHANT_NAME"]
                            ?? _configuration["KHQR:MerchantName"]
                            ?? "SOPHIRAM SORN";

            var storeLabel = _configuration["BAKONG_STORE_LABEL"]
                          ?? _configuration["KHQR_STORE_LABEL"]
                          ?? _configuration["KHQR:StoreLabel"]
                          ?? "RS Clothing Store";

            var phone = _configuration["BAKONG_PHONE"]
                     ?? _configuration["KHQR_PHONE"]
                     ?? _configuration["KHQR:Phone"]
                     ?? "0969144183";

            var city = _configuration["BAKONG_MERCHANT_CITY"]
                    ?? _configuration["KHQR_MERCHANT_CITY"]
                    ?? _configuration["KHQR_CITY"]
                    ?? _configuration["KHQR:MerchantCity"]
                    ?? "Phnom Penh";

            var currency = _configuration["BAKONG_CURRENCY"]
                        ?? _configuration["KHQR_CURRENCY"]
                        ?? _configuration["KHQR:Currency"]
                        ?? "USD";

            var rateStr = _configuration["BAKONG_USD_TO_KHR_RATE"]
                       ?? _configuration["KHQR_USD_TO_KHR_RATE"]
                       ?? _configuration["KHQR:UsdToKhrRate"];

            if (!decimal.TryParse(rateStr, out var rate) || rate <= 0)
            {
                rate = 4100m;
            }

            return new KhqrConfig
            {
                BaseUrl = baseUrl,
                Token = token,
                BakongAccount = bakongAccount,
                MerchantName = merchantName,
                StoreLabel = storeLabel,
                Phone = phone,
                City = city,
                Currency = currency,
                UsdToKhrRate = rate
            };
        }

        // ─── Helpers ──────────────────────────────────────────────────

        private static string BuildTLV(string tag, string value)
        {
            var len = Encoding.UTF8.GetByteCount(value).ToString("D2");
            return $"{tag}{len}{value}";
        }

        private static string CleanAscii(string text, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(text)) return "STORE";
            var ascii = new string(text.Where(c => c < 128 && (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(ascii)) ascii = "STORE";
            return ascii.Length > maxLen ? ascii[..maxLen] : ascii;
        }

        private static string ComputeCrc16Ccitt(string data)
        {
            ushort crc = 0xFFFF;
            var bytes = Encoding.UTF8.GetBytes(data);
            foreach (var b in bytes)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc.ToString("X4");
        }

        private static string ComputeMd5(string input)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        // Helper to format USD amount like the official SDK (strip trailing zeros)
        private static string TrimTrailingZeros(decimal amount)
        {
            // Remove trailing zeros and unnecessary decimal point
            var s = amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (s.Contains('.'))
            {
                s = s.TrimEnd('0');
                if (s.EndsWith('.')) s = s.TrimEnd('.');
            }
            return s;
        }

        // Build Tag 99 (timestamp) with creation and expiration sub‑tags
        private static string BuildTag99()
        {
            var creation = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var expiration = creation + 15 * 60 * 1000; // 15 minutes validity
            var sb = new StringBuilder();
            sb.Append(BuildTLV("00", creation.ToString())); // sub‑tag 00 creation timestamp
            sb.Append(BuildTLV("01", expiration.ToString())); // sub‑tag 01 expiration timestamp
            return BuildTLV("99", sb.ToString());
        }
    }
}
