using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class DeliveryController : Controller
    {
        private readonly IDeliveryService _deliveryService;

        public DeliveryController(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var methods = await _deliveryService.GetAllDeliveryMethodsAsync();
            var branches = await _deliveryService.GetBranchesByCarrierAsync("VETExpress");
            ViewBag.BranchesCount = branches.Count;
            return View(methods);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMethod(DeliveryMethod model)
        {
            var success = await _deliveryService.UpdateDeliveryMethodAsync(model);
            if (success)
            {
                TempData["Success"] = $"Delivery method '{model.Name}' updated successfully.";
            }
            else
            {
                TempData["Error"] = "Unable to update delivery method.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Branches()
        {
            var branches = await _deliveryService.GetBranchesByCarrierAsync("VETExpress");
            return View(branches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBranch(DeliveryBranch branch)
        {
            if (string.IsNullOrWhiteSpace(branch.Province) || string.IsNullOrWhiteSpace(branch.BranchName))
            {
                TempData["Error"] = "Province and Branch Name are required.";
                return RedirectToAction(nameof(Branches));
            }

            branch.CarrierCode = "VETExpress";
            await _deliveryService.AddBranchAsync(branch);
            TempData["Success"] = $"VET Express Branch '{branch.BranchName}' in {branch.Province} added successfully.";
            return RedirectToAction(nameof(Branches));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBranch(Guid id)
        {
            await _deliveryService.DeleteBranchAsync(id);
            TempData["Success"] = "Branch removed successfully.";
            return RedirectToAction(nameof(Branches));
        }
    }
}
