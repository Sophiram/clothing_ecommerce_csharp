using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService _avatarService;

        public CustomersController(
            ICustomerService customerService,
            WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService avatarService)
        {
            _customerService = customerService;
            _avatarService = avatarService;
        }

        // =====================================================
        // 1. INDEX: LIST ALL CUSTOMERS
        // GET: /Admin/Customers
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, CustomerStatus? status = null)
        {
            var (customers, totalCount, activeCount) = await _customerService.GetCustomersAsync(search, status);

            var avatarMap = new Dictionary<Guid, string?>();
            foreach (var c in customers)
            {
                if (!string.IsNullOrEmpty(c.ApplicationUserId))
                {
                    avatarMap[c.Id] = _avatarService.GetAvatarUrl(c.ApplicationUserId);
                }
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.TotalCount = totalCount;
            ViewBag.ActiveCount = activeCount;
            ViewBag.CustomerAvatars = avatarMap;

            return View(customers);
        }

        // =====================================================
        // 2. DETAILS: CUSTOMER OVERVIEW
        // GET: /Admin/Customers/Details/{id}
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var customer = await _customerService.GetCustomerDetailsAsync(id.Value);
            if (customer == null) return NotFound();

            if (!string.IsNullOrEmpty(customer.ApplicationUserId))
            {
                ViewBag.AvatarUrl = _avatarService.GetAvatarUrl(customer.ApplicationUserId);
            }

            return View(customer);
        }

        // =====================================================
        // 3. EDIT: MODIFY CUSTOMER INFO / STATUS
        // GET & POST: /Admin/Customers/Edit/{id}
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var customer = await _customerService.GetCustomerByIdAsync(id.Value);
            if (customer == null) return NotFound();

            if (!string.IsNullOrEmpty(customer.ApplicationUserId))
            {
                ViewBag.AvatarUrl = _avatarService.GetAvatarUrl(customer.ApplicationUserId);
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,Email,Phone,Status")] Customer model, IFormFile? avatarFile)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingCustomer = await _customerService.GetCustomerByIdAsync(id);
                if (existingCustomer != null && !string.IsNullOrEmpty(existingCustomer.ApplicationUserId) && avatarFile != null && avatarFile.Length > 0)
                {
                    await _avatarService.UploadAvatarAsync(existingCustomer.ApplicationUserId, avatarFile);
                }

                var result = await _customerService.UpdateCustomerAsync(
                    id,
                    model,
                    User.Identity?.Name,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                if (!result.Success)
                {
                    return NotFound();
                }

                TempData["Success"] = result.SuccessMessage;
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // =====================================================
        // 4. TOGGLE STATUS
        // POST: /Admin/Customers/ToggleStatus/{id}
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var result = await _customerService.ToggleCustomerStatusAsync(
                id,
                User.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (!result.Success)
            {
                return NotFound();
            }

            TempData["Success"] = result.SuccessMessage;
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // 5. DELETE: REMOVE CUSTOMER RECORD
        // POST: /Admin/Customers/Delete/{id}
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _customerService.DeleteCustomerAsync(
                id,
                User.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (!result.Success)
            {
                if (result.ErrorMessage == "Customer not found.")
                {
                    return NotFound();
                }
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = result.SuccessMessage;
            return RedirectToAction(nameof(Index));
        }
    }
}
