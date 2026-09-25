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
        private readonly IWebHostEnvironment _environment;

        public KhqrController(IKhqrService khqrService, IWebHostEnvironment environment)
        {
            _khqrService = khqrService;
            _environment = environment;
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
        // GET & POST: /Khqr/CheckPayment & /Khqr/CheckTransaction
        // Polls the Bakong Open API check_transaction_by_md5 endpoint
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> CheckPayment([FromQuery] string? md5, [FromQuery] bool simulate = false)
        {
            if (simulate)
            {
                if (!_environment.IsDevelopment() || (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin")))
                {
                    return NotFound(new { error = "Payment simulation is disabled." });
                }

                return Ok(new
                {
                    paid = true,
                    status = "PAID",
                    responseCode = 0,
                    message = "Simulated local payment verified.",
                    data = new
                    {
                        hash = "sim_" + Guid.NewGuid().ToString("N")[..16],
                        fromAccountId = "client_mobile@abaa",
                        toAccountId = "sorn_sophiram@bkrt",
                        amount = 0,
                        currency = "USD",
                        description = "Simulated Auto-Payment Test",
                        createdDateMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }
                });
            }

            if (string.IsNullOrWhiteSpace(md5))
            {
                return BadRequest(new { error = "MD5 hash is required." });
            }

            var config = _khqrService.GetConfig();
            var result = await _khqrService.CheckTransactionByMd5Async(md5);
            return Ok(new
            {
                paid = result.IsPaid,
                status = result.IsPaid ? "PAID" : "PENDING",
                responseCode = result.ResponseCode,
                message = result.ResponseMessage,
                tokenConfigured = !string.IsNullOrWhiteSpace(config.Token),
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
                status = result.IsPaid ? "PAID" : "PENDING",
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
