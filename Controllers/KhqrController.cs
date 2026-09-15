using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers
{
    /// <summary>
    /// API controller exposing Bakong KHQR generation and live verification endpoints.
    /// </summary>
    [Authorize]
    public class KhqrController : Controller
    {
        private readonly IKhqrService _khqrService;

        public KhqrController(IKhqrService khqrService)
        {
            _khqrService = khqrService;
        }

        // ─────────────────────────────────────────────────────────────
        // GET: /Khqr/Generate
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Generate(decimal amount, string currency = "USD", string? reference = null)
        {
            if (amount <= 0)
            {
                return BadRequest(new { error = "Amount must be greater than zero." });
            }

            try
            {
                var response = _khqrService.GenerateKhqr(amount, currency, reference);
                return Ok(new
                {
                    qrString = response.QrString,
                    md5 = response.Md5,
                    amount = response.Amount,
                    khrAmount = response.KhrAmount,
                    currency = response.Currency,
                    merchant = response.MerchantName,
                    storeLabel = response.StoreLabel,
                    city = response.MerchantCity,
                    bakongId = response.BakongId,
                    phone = response.Phone,
                    reference = response.Reference,
                    abaDeepLink = response.AbaDeepLink
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // POST: /Khqr/CheckTransaction
        // Polls the Bakong Open API check_transaction_by_md5 endpoint
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CheckTransaction([FromBody] CheckTransactionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Md5))
            {
                return BadRequest(new { error = "MD5 hash is required." });
            }

            var result = await _khqrService.CheckTransactionByMd5Async(request.Md5);

            return Ok(new
            {
                paid = result.IsPaid,
                responseCode = result.ResponseCode,
                message = result.ResponseMessage,
                data = result.IsPaid ? new
                {
                    hash = result.Hash,
                    fromAccountId = result.FromAccountId,
                    toAccountId = result.ToAccountId,
                    amount = result.Amount,
                    currency = result.Currency,
                    description = result.Description,
                    createdDateMs = result.CreatedDateMs
                } : null
            });
        }
    }

    public class CheckTransactionRequest
    {
        public string Md5 { get; set; } = string.Empty;
    }
}
