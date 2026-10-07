using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ShipmentsController : Controller
    {
        private readonly IShipmentService _shipmentService;

        public ShipmentsController(IShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }

        // =========================================================
        // GET: /Admin/Shipments
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, ShipmentStatus? status = null)
        {
            var shipments = await _shipmentService.GetAllShipmentsAsync(search, status);

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Statuses = Enum.GetValues<ShipmentStatus>()
                .Select(s => new SelectListItem
                {
                    Text = s.ToString(),
                    Value = s.ToString(),
                    Selected = (status == s)
                }).ToList();

            ViewBag.TotalCount = shipments.Count;
            ViewBag.PendingCount = shipments.Count(s => s.ShipmentStatus == ShipmentStatus.Pending);
            ViewBag.InTransitCount = shipments.Count(s => s.ShipmentStatus == ShipmentStatus.InTransit);
            ViewBag.DeliveredCount = shipments.Count(s => s.ShipmentStatus == ShipmentStatus.Delivered);

            return View(shipments);
        }

        // =========================================================
        // GET: /Admin/Shipments/Details/{id}
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var shipment = await _shipmentService.GetShipmentByIdAsync(id.Value);
            if (shipment == null) return NotFound();

            return View(shipment);
        }

        // =========================================================
        // GET: /Admin/Shipments/Edit/{id}
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var shipment = await _shipmentService.GetShipmentByIdAsync(id.Value);
            if (shipment == null) return NotFound();

            ViewBag.Statuses = new SelectList(Enum.GetValues<ShipmentStatus>(), shipment.ShipmentStatus);
            return View(shipment);
        }

        // =========================================================
        // POST: /Admin/Shipments/Edit/{id}
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,TrackingNumber,ShippingCompany,ShipmentStatus,ShippedAt,DeliveredAt")] Shipment model)
        {
            if (id != model.Id) return NotFound();

            var success = await _shipmentService.UpdateShipmentAsync(
                id,
                model.TrackingNumber,
                model.ShippingCompany,
                model.ShipmentStatus,
                model.ShippedAt,
                model.DeliveredAt
            );

            if (!success) return NotFound();

            TempData["Success"] = "Shipment details updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // POST: /Admin/Shipments/QuickUpdate
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUpdate(Guid id, ShipmentStatus status, string? trackingNumber = null, string? returnUrl = null)
        {
            var success = await _shipmentService.QuickUpdateStatusAsync(id, status, trackingNumber);
            if (!success) return NotFound();

            TempData["Success"] = $"Shipment status updated to {status}.";
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // POST: /Admin/Shipments/Fulfill
        // Dispatch order fulfillment directly from Store Dashboard
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fulfill(Guid orderId, string? carrier = null, string? trackingNumber = null, string? returnUrl = null)
        {
            var success = await _shipmentService.FulfillOrderAsync(orderId, carrier ?? "Standard Courier", trackingNumber ?? string.Empty);
            if (!success)
            {
                TempData["Error"] = "Unable to process fulfillment for the selected order.";
            }
            else
            {
                TempData["Success"] = $"Order #{orderId.ToString()[..8].ToUpper()} has been dispatched and marked as Shipped!";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // POST: /Admin/Shipments/Delete/{id}
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _shipmentService.DeleteShipmentAsync(id);
            if (success)
            {
                TempData["Success"] = "Shipment record deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
