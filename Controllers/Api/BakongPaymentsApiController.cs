using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers.Api
{
    [ApiController]
    [Route("api/payments")]
    public class BakongPaymentsApiController : ControllerBase
    {
        private readonly IBakongPaymentService _bakongPaymentService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BakongPaymentsApiController> _logger;

        public BakongPaymentsApiController(
            IBakongPaymentService bakongPaymentService,
            IWebHostEnvironment environment,
            ILogger<BakongPaymentsApiController> logger)
        {
            _bakongPaymentService = bakongPaymentService;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/payments/bakong/create
        /// Creates a unique dynamic Bakong KHQR payment attempt for an order.
        /// Amount is strictly retrieved and validated from the database.
        /// </summary>
        [HttpPost("bakong/create")]
        public async Task<IActionResult> Create([FromBody] CreateBakongPaymentRequest request)
        {
            if (request == null || request.OrderId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "Valid Order ID is required." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _bakongPaymentService.CreatePaymentAttemptAsync(
                request.OrderId,
                string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
                userId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// GET /api/payments/{paymentId}/status
        /// Polls or queries the current payment status and automatically validates with Bakong Open API.
        /// </summary>
        [HttpGet("{paymentId:guid}/status")]
        public async Task<IActionResult> GetStatus(Guid paymentId)
        {
            var result = await _bakongPaymentService.GetPaymentStatusAsync(paymentId);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/payments/{paymentId}/cancel
        /// Cancels a pending payment attempt.
        /// </summary>
        [HttpPost("{paymentId:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid paymentId)
        {
            var result = await _bakongPaymentService.CancelPaymentAsync(paymentId);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/payments/{paymentId}/simulate-confirm
        /// Sandbox simulator confirmation for local test environments (Development only, Admin only).
        /// </summary>
        [HttpPost("{paymentId:guid}/simulate-confirm")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> SimulateConfirm(Guid paymentId)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound(new { success = false, message = "Simulation is only permitted in development environments." });
            }

            var result = await _bakongPaymentService.ConfirmSimulatedPaymentAsync(paymentId);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/payments/bakong/callback
        /// Webhook callback endpoint for Bakong or gateway payment notification.
        /// </summary>
        [HttpPost("bakong/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromBody] System.Text.Json.JsonElement? payload)
        {
            _logger.LogInformation("Received external Bakong payment callback: {Payload}", payload?.ToString());
            return Ok(new { responseCode = 0, responseMessage = "Callback received" });
        }
    }
}
