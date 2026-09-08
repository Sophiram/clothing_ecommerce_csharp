using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const decimal DeliveryFee = 3.00m;

        public CartController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // GET CUSTOMER
        // =========================================================

        private async Task<Customer?> GetCustomer()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return null;

            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == user.Email);
        }

        // =========================================================
        // GET PAYMENT METHODS
        // =========================================================

        private async Task LoadPaymentMethods()
        {
            ViewBag.PaymentMethods = await _context.PaymentMethods
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        // =========================================================
        // CART INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customer = await GetCustomer();

            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var cart = await _context.Carts
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

                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customer.Id);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var subtotal = cart.Items
                .Where(i => i.Variant != null)
                .Sum(i => i.Quantity * i.Variant!.Price);

            var deliveryFee = subtotal > 0
                ? DeliveryFee
                : 0m;

            var model = new CartViewModel
            {
                Cart = cart,
                SubTotal = subtotal,
                DeliveryFee = deliveryFee,
                Total = subtotal + deliveryFee
            };

            return View(model);
        }

        // =========================================================
        // ADD TO CART
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            Guid variantId,
            int quantity = 1)
        {
            var customer = await GetCustomer();

            if (customer == null)
                return RedirectToAction("Index", "Home");

            if (quantity < 1)
                quantity = 1;

            var variant = await _context.ProductVariants
                .Include(v => v.Inventory)
                .FirstOrDefaultAsync(v => v.Id == variantId);

            if (variant == null)
            {
                TempData["Error"] = "Product variant was not found.";
                return RedirectToAction("Index", "Shop");
            }

            var available =
                variant.Inventory?.AvailableQuantity ?? 0;

            if (available <= 0)
            {
                TempData["Error"] = "This product is out of stock.";
                return RedirectToAction("Details", "Shop", new { id = variant.ProductId });
            }

            if (quantity > available)
            {
                TempData["Error"] = $"Only {available} item(s) available.";
                return RedirectToAction("Details", "Shop", new { id = variant.ProductId });
            }

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customer.Id);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(cart);

                await _context.SaveChangesAsync();
            }

            var item = await _context.CartItems
                .FirstOrDefaultAsync(i =>
                    i.CartId == cart.Id &&
                    i.VariantId == variantId);

            if (item == null)
            {
                item = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    VariantId = variantId,
                    Quantity = quantity
                };

                _context.CartItems.Add(item);
            }
            else
            {
                var newQuantity = item.Quantity + quantity;

                if (newQuantity > available)
                {
                    TempData["Error"] =
                        $"Only {available} item(s) available.";

                    return RedirectToAction("Index");
                }

                item.Quantity = newQuantity;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Product added to cart.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                var totalItems = await _context.CartItems
                    .Where(ci => ci.Cart.CustomerId == customer.Id)
                    .SumAsync(ci => (int?)ci.Quantity) ?? 0;

                return Json(new { 
                    success = true, 
                    message = "Product added to cart!", 
                    cartCount = totalItems 
                });
            }

            return RedirectToAction("Index");
        }

        // =========================================================
        // GET CART COUNT (AJAX)
        // =========================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCount()
        {
            if (User.Identity?.IsAuthenticated != true)
                return Json(new { count = 0 });

            var customer = await GetCustomer();
            if (customer == null)
                return Json(new { count = 0 });

            var count = await _context.CartItems
                .Where(i => i.Cart.CustomerId == customer.Id)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            return Json(new { count });
        }

        // =========================================================
        // UPDATE CART
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            Guid id,
            int quantity)
        {
            var customer = await GetCustomer();

            if (customer == null)
                return RedirectToAction("Index", "Home");

            var item = await _context.CartItems
                .Include(i => i.Cart)
                .Include(i => i.Variant)
                    .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.Cart.CustomerId == customer.Id);

            if (item == null)
                return NotFound();

            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                var available =
                    item.Variant?.Inventory?.AvailableQuantity ?? 0;

                if (available <= 0)
                {
                    TempData["Error"] =
                        "This product is out of stock.";

                    _context.CartItems.Remove(item);
                }
                else if (quantity > available)
                {
                    item.Quantity = available;

                    TempData["Error"] =
                        $"Only {available} item(s) available.";
                }
                else
                {
                    item.Quantity = quantity;

                    TempData["Success"] =
                        "Cart updated successfully.";
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // =========================================================
        // REMOVE FROM CART
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid id)
        {
            var customer = await GetCustomer();

            if (customer == null)
                return RedirectToAction("Index", "Home");

            var item = await _context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i =>
                    i.Id == id &&
                    i.Cart.CustomerId == customer.Id);

            if (item != null)
            {
                _context.CartItems.Remove(item);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Item removed from cart.";
            }

            return RedirectToAction("Index");
        }

        // =========================================================
        // CHECKOUT GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var customer = await GetCustomer();

            if (customer == null)
            {
                TempData["Error"] =
                    "Customer profile was not found.";

                return RedirectToAction("Index", "Home");
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)

                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)

                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)

                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customer.Id);

            if (cart == null || cart.Items.Count == 0)
            {
                TempData["Error"] =
                    "Your cart is empty.";

                return RedirectToAction("Index");
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.CustomerId == customer.Id &&
                    a.IsDefault);

            address ??= await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.CustomerId == customer.Id);

            var model = new CheckoutViewModel
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Phone = customer.Phone,
                Email = customer.Email
            };

            if (address != null)
            {
                model.Province = address.Province;
                model.City = address.City;
                model.Street = address.Street;
                model.PostalCode = address.PostalCode;
            }

            // Load checkout items
            foreach (var item in cart.Items)
            {
                if (item.Variant == null)
                    continue;

                var variant = item.Variant;
                var product = variant.Product;

                if (product == null)
                    continue;

                var image = product.Images?
                    .FirstOrDefault(i => i.IsPrimary)
                    ?? product.Images?.FirstOrDefault();

                model.Items.Add(new CheckoutItem
                {
                    VariantId = variant.Id,
                    ProductName = product.Name,
                    SKU = variant.SKU,

                    Size = variant.Size?.Name
                        ?? string.Empty,

                    Color = variant.Color?.Name
                        ?? string.Empty,

                    ImageUrl = image?.ImageUrl
                        ?? "/images/no-image.png",

                    UnitPrice = variant.Price,
                    Quantity = item.Quantity
                });
            }

            model.SubTotal =
                model.Items.Sum(i =>
                    i.UnitPrice * i.Quantity);

            model.DeliveryFee =
                model.Items.Count > 0
                    ? DeliveryFee
                    : 0m;

            model.Total =
                model.SubTotal +
                model.DeliveryFee;

            // Load payment methods
            await LoadPaymentMethods();

            return View(model);
        }

        // =========================================================
        // CHECKOUT POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            CheckoutViewModel model)
        {
            // -----------------------------------------------------
            // CUSTOMER
            // -----------------------------------------------------

            var customer = await GetCustomer();

            if (customer == null)
            {
                TempData["Error"] =
                    "Customer profile was not found.";

                return RedirectToAction("Index", "Home");
            }

            // -----------------------------------------------------
            // CART
            // -----------------------------------------------------

            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)

                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)

                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customer.Id);

            if (cart == null || cart.Items.Count == 0)
            {
                TempData["Error"] =
                    "Your cart is empty.";

                return RedirectToAction("Index");
            }

            // -----------------------------------------------------
            // PAYMENT METHOD
            // -----------------------------------------------------

            if (!model.PaymentMethodId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMethodId),
                    "Please select a payment method.");

                await LoadPaymentMethods();

                return View(model);
            }

            var paymentMethod =
                await _context.PaymentMethods
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.PaymentMethodId.Value &&
                        x.IsActive);

            if (paymentMethod == null)
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMethodId),
                    "The selected payment method is not available.");

                await LoadPaymentMethods();

                return View(model);
            }

            // -----------------------------------------------------
            // STOCK CHECK
            // -----------------------------------------------------

            foreach (var item in cart.Items)
            {
                if (item.Variant == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "One of the products in your cart is no longer available.");

                    continue;
                }

                var available =
                    item.Variant.Inventory?.AvailableQuantity ?? 0;

                if (available < item.Quantity)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Not enough stock for {item.Variant.Product?.Name ?? "a product"}.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadPaymentMethods();

                return View(model);
            }

            // -----------------------------------------------------
            // ADDRESS
            // -----------------------------------------------------

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.CustomerId == customer.Id &&
                    a.Street == model.Street &&
                    a.City == model.City &&
                    a.Province == model.Province);

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

            // -----------------------------------------------------
            // UPDATE CUSTOMER
            // -----------------------------------------------------

            customer.FirstName = model.FirstName;
            customer.LastName = model.LastName;
            customer.Phone = model.Phone;
            customer.Email = model.Email;

            // -----------------------------------------------------
            // CALCULATE TOTAL
            // -----------------------------------------------------

            var subtotal = cart.Items
                .Where(i => i.Variant != null)
                .Sum(i =>
                    i.Quantity * i.Variant!.Price);

            var deliveryFee =
                subtotal > 0
                    ? DeliveryFee
                    : 0m;

            var total =
                subtotal + deliveryFee;

            // -----------------------------------------------------
            // CREATE ORDER
            // -----------------------------------------------------

            var order = new Order
            {
                Id = Guid.NewGuid(),

                CustomerId = customer.Id,

                AddressId = address.Id,

                OrderDate = DateTime.UtcNow,

                Status = Data.Enums.OrderStatus.Pending,

                TotalAmount = total
            };

            _context.Orders.Add(order);

            // -----------------------------------------------------
            // CREATE ORDER ITEMS
            // -----------------------------------------------------

            foreach (var item in cart.Items)
            {
                if (item.Variant == null)
                    continue;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),

                    OrderId = order.Id,

                    VariantId = item.VariantId,

                    Quantity = item.Quantity,

                    UnitPrice = item.Variant.Price
                };

                _context.OrderItems.Add(orderItem);
            }

            // -----------------------------------------------------
            // CREATE PAYMENT
            // -----------------------------------------------------

            var payment = new Payment
            {
                Id = Guid.NewGuid(),

                OrderId = order.Id,

                PaymentMethodId =
                    paymentMethod.Id,

                PaymentStatus =
                    Data.Enums.PaymentStatus.Pending,

                Amount = total,

                PaidAt = null
            };

            _context.Payments.Add(payment);

            // -----------------------------------------------------
            // CREATE SHIPMENT
            // -----------------------------------------------------

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),

                OrderId = order.Id,

                ShippingCompany = "J&T Express",

                TrackingNumber = string.Empty,

                ShipmentStatus =
                    Data.Enums.ShipmentStatus.Pending,

                ShippedAt = null,

                DeliveredAt = null
            };

            _context.Shipments.Add(shipment);

            // -----------------------------------------------------
            // CLEAR CART
            // -----------------------------------------------------

            _context.CartItems.RemoveRange(cart.Items);

            // -----------------------------------------------------
            // SAVE
            // -----------------------------------------------------

            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            TempData["Success"] =
                "Your order has been placed successfully.";

            return RedirectToAction(
                "Details",
                "Orders",
                new { id = order.Id });
        }
    }
}