using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Payments;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IKhqrService _khqrService;
        private readonly IWebHostEnvironment _environment;

        public PaymentsController(IKhqrService khqrService, IWebHostEnvironment environment)
        {
            _khqrService = khqrService;
            _environment = environment;
        }

        [HttpPost("khqr/generate")]
        public IActionResult GenerateKhqr([FromBody] KhqrGenerateRequestDto request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(ApiResponse<KhqrGenerateResponseDto>.Fail("Amount must be greater than zero."));
            }

            try
            {
                var response = _khqrService.GenerateKhqr(request.Amount, request.Currency ?? "USD", request.Reference ?? request.OrderId);
                var dto = new KhqrGenerateResponseDto
                {
                    QrString = response.QrString,
                    Md5 = response.Md5,
                    Amount = response.Amount,
                    KhrAmount = response.KhrAmount,
                    Currency = response.Currency,
                    MerchantName = response.MerchantName,
                    StoreLabel = response.StoreLabel,
                    MerchantCity = response.MerchantCity,
                    BakongId = response.BakongId,
                    Phone = response.Phone,
                    Reference = response.Reference,
                    AbaDeepLink = response.AbaDeepLink
                };

                return Ok(ApiResponse<KhqrGenerateResponseDto>.Ok(dto));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<KhqrGenerateResponseDto>.Fail(ex.Message));
            }
        }

        [HttpGet("khqr/status/{md5}")]
        public async Task<IActionResult> CheckPaymentStatus(string md5, [FromQuery] bool simulate = false)
        {
            if (simulate)
            {
                if (!_environment.IsDevelopment() || (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin")))
                {
                    return NotFound(ApiResponse<PaymentStatusResponseDto>.Fail("Payment simulation is disabled."));
                }

                return Ok(ApiResponse<PaymentStatusResponseDto>.Ok(new PaymentStatusResponseDto
                {
                    Paid = true,
                    Status = "PAID",
                    ResponseCode = 0,
                    Message = "Simulated development payment verified.",
                    Hash = "sim_" + Guid.NewGuid().ToString("N")[..16],
                    Amount = 0,
                    Currency = "USD"
                }));
            }

            if (string.IsNullOrWhiteSpace(md5))
            {
                return BadRequest(ApiResponse<PaymentStatusResponseDto>.Fail("MD5 hash is required."));
            }

            var result = await _khqrService.CheckTransactionByMd5Async(md5);
            var dto = new PaymentStatusResponseDto
            {
                Paid = result.IsPaid,
                Status = result.IsPaid ? "PAID" : "PENDING",
                ResponseCode = result.ResponseCode,
                Message = result.ResponseMessage,
                Hash = result.Hash,
                FromAccountId = result.FromAccountId,
                ToAccountId = result.ToAccountId,
                Amount = result.Amount ?? 0,
                Currency = result.Currency ?? "USD",
                CreatedDateMs = result.CreatedDateMs
            };

            return Ok(ApiResponse<PaymentStatusResponseDto>.Ok(dto));
        }
    }
}
