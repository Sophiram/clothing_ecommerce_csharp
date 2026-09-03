using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const decimal DeliveryFee = 3.00m;

        public CheckoutController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // GET: /Checkout/Index
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customer = await GetCustomerAsync();

            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var cart = await GetCartAsync(customer.Id);

            if (cart == null || cart.Items.Count == 0)
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            var address = await GetCustomerAddressAsync(customer.Id);

            var paymentMethods = await _context.PaymentMethods
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var model = new CheckoutViewModel
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Phone = customer.Phone,
                Email = customer.Email,

                Province = address?.Province ?? string.Empty,
                City = address?.City ?? string.Empty,
                Street = address?.Street ?? string.Empty,
                PostalCode = address?.PostalCode ?? string.Empty
            };

            // Select first active payment method by default
            if (paymentMethods.Count > 0)
            {
                model.PaymentMethodId = paymentMethods[0].Id;
            }

            ViewBag.PaymentMethods = paymentMethods;

            BuildCheckoutItems(model, cart);

            return View(model);
        }


        // =========================================================
        // POST: /Checkout/Index
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var customer = await GetCustomerAsync();

            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var cart = await GetCartAsync(customer.Id);

            if (cart == null || cart.Items.Count == 0)
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }


            // =====================================================
            // BUILD CHECKOUT SUMMARY
            // =====================================================

            BuildCheckoutItems(model, cart);

            model.DeliveryFee =
                model.Items.Count > 0
                    ? DeliveryFee
                    : 0m;

            model.SubTotal =
                model.Items.Sum(i => i.Subtotal);

            model.Total =
                model.SubTotal + model.DeliveryFee;


            // =====================================================
            // CHECK STOCK
            // =====================================================

            foreach (var cartItem in cart.Items)
            {
                if (cartItem.Variant == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "One of the products in your cart is no longer available.");

                    continue;
                }

                var available =
                    cartItem.Variant.Inventory?.AvailableQuantity ?? 0;

                if (available < cartItem.Quantity)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Not enough stock for {cartItem.Variant.Product?.Name ?? "a product"}.");
                }
            }


            // =====================================================
            // PAYMENT METHOD
            // =====================================================

            PaymentMethod? paymentMethod = null;

            if (model.PaymentMethodId.HasValue)
            {
                paymentMethod = await _context.PaymentMethods
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.PaymentMethodId.Value &&
                        x.IsActive);
            }

            if (paymentMethod == null)
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMethodId),
                    "Please select a valid payment method.");
            }


            // =====================================================
            // VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                ViewBag.PaymentMethods = await _context.PaymentMethods
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Name)
                    .ToListAsync();

                return View(model);
            }


            // =====================================================
            // ADDRESS
            // =====================================================

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.CustomerId == customer.Id &&
                    a.Province == model.Province &&
                    a.City == model.City &&
                    a.Street == model.Street);

            if (address == null)
            {
                var hasExistingAddress =
                    await _context.Addresses
                        .AnyAsync(a =>
                            a.CustomerId == customer.Id);

                address = new Address
                {
                    Id = Guid.NewGuid(),

                    CustomerId = customer.Id,

                    Province = model.Province,
                    City = model.City,
                    Street = model.Street,
                    PostalCode = model.PostalCode,

                    IsDefault = !hasExistingAddress
                };

                _context.Addresses.Add(address);
            }
            else
            {
                address.PostalCode = model.PostalCode;
            }


            // =====================================================
            // UPDATE CUSTOMER
            // =====================================================

            customer.FirstName = model.FirstName;
            customer.LastName = model.LastName;
            customer.Phone = model.Phone;
            customer.Email = model.Email;


            // =====================================================
            // CREATE ORDER
            // =====================================================

            var order = new Order
            {
                Id = Guid.NewGuid(),

                CustomerId = customer.Id,

                AddressId = address.Id,

                OrderDate = DateTime.UtcNow,

                Status = OrderStatus.Pending,

                TotalAmount = model.Total
            };

            _context.Orders.Add(order);


            // =====================================================
            // CREATE ORDER ITEMS
            // =====================================================

            foreach (var cartItem in cart.Items)
            {
                if (cartItem.Variant == null)
                    continue;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),

                    OrderId = order.Id,

                    VariantId = cartItem.VariantId,

                    Quantity = cartItem.Quantity,

                    UnitPrice = cartItem.Variant.Price
                };

                _context.OrderItems.Add(orderItem);
            }


            // =====================================================
            // CREATE PAYMENT
            // =====================================================

            var payment = new Payment
            {
                Id = Guid.NewGuid(),

                OrderId = order.Id,

                PaymentMethodId = paymentMethod!.Id,

                PaymentStatus = PaymentStatus.Pending,

                Amount = model.Total,

                PaidAt = null
            };

            _context.Payments.Add(payment);


            // =====================================================
            // CREATE SHIPMENT
            // =====================================================

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),

                OrderId = order.Id,

                ShippingCompany = "J&T Express",

                TrackingNumber = string.Empty,

                ShipmentStatus = ShipmentStatus.Pending,

                ShippedAt = null,

                DeliveredAt = null
            };

            _context.Shipments.Add(shipment);


            // =====================================================
            // CLEAR CART
            // =====================================================

            _context.CartItems.RemoveRange(cart.Items);


            // =====================================================
            // SAVE
            // =====================================================

            await _context.SaveChangesAsync();


            // =====================================================
            // SUCCESS
            // =====================================================

            TempData["Success"] =
                "Your order has been placed successfully.";

            return RedirectToAction(
                "Details",
                "Orders",
                new { id = order.Id });
        }


        // =========================================================
        // GET CUSTOMER
        // =========================================================

        private async Task<Customer?> GetCustomerAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null ||
                string.IsNullOrWhiteSpace(user.Email))
            {
                return null;
            }

            return await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.Email == user.Email);
        }


        // =========================================================
        // GET CART
        // =========================================================

        private async Task<Cart?> GetCartAsync(Guid customerId)
        {
            return await _context.Carts

                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)

                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Brand)

                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)

                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)

                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)

                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customerId);
        }


        // =========================================================
        // GET CUSTOMER ADDRESS
        // =========================================================

        private async Task<Address?> GetCustomerAddressAsync(
            Guid customerId)
        {
            return await _context.Addresses
                .Where(a =>
                    a.CustomerId == customerId)
                .OrderByDescending(a =>
                    a.IsDefault)
                .FirstOrDefaultAsync();
        }


        // =========================================================
        // BUILD CHECKOUT ITEMS
        // =========================================================

        private static void BuildCheckoutItems(
            CheckoutViewModel model,
            Cart cart)
        {
            model.Items.Clear();

            foreach (var item in cart.Items)
            {
                var variant = item.Variant;

                if (variant == null)
                    continue;

                var product = variant.Product;

                if (product == null)
                    continue;

                var image =
                    product.Images?
                        .FirstOrDefault(i => i.IsPrimary)
                    ??
                    product.Images?
                        .FirstOrDefault();

                model.Items.Add(new CheckoutItem
                {
                    VariantId = variant.Id,

                    ProductName = product.Name,

                    SKU = variant.SKU,

                    Size = variant.Size?.Name
                        ?? string.Empty,

                    Color = variant.Color?.Name
                        ?? string.Empty,

                    ImageUrl =
                        image?.ImageUrl
                        ??
                        "https://via.placeholder.com/600x700?text=No+Image",

                    UnitPrice = variant.Price,

                    Quantity = item.Quantity
                });
            }

            model.SubTotal =
                model.Items.Sum(i => i.Subtotal);

            model.DeliveryFee =
                model.Items.Count > 0
                    ? DeliveryFee
                    : 0m;

            model.Total =
                model.SubTotal +
                model.DeliveryFee;
        }
    }
}
