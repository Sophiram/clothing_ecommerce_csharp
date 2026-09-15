using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private const decimal DeliveryFee = 3.00m;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PREPARE CHECKOUT
        // =========================================================
        public async Task<CheckoutViewModel?> PrepareCheckoutAsync(Guid customerId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null) return null;

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
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null || !cart.Items.Any())
                return null;

            var address = await _context.Addresses
                .Where(a => a.CustomerId == customerId)
                .OrderByDescending(a => a.IsDefault)
                .FirstOrDefaultAsync();

            var paymentMethods = await _context.PaymentMethods
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Name)
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
                PostalCode = address?.PostalCode ?? string.Empty,
                PaymentMethodId = paymentMethods.FirstOrDefault()?.Id
            };

            foreach (var item in cart.Items)
            {
                if (item.Variant == null || item.Variant.Product == null) continue;

                var variant = item.Variant;
                var product = variant.Product;
                var image = product.Images?.FirstOrDefault(i => i.IsPrimary) ?? product.Images?.FirstOrDefault();

                model.Items.Add(new CheckoutItem
                {
                    VariantId = variant.Id,
                    ProductName = product.Name,
                    SKU = variant.SKU,
                    Size = variant.Size?.Name ?? string.Empty,
                    Color = variant.Color?.Name ?? string.Empty,
                    ImageUrl = image?.ImageUrl ?? "/images/no-image.png",
                    UnitPrice = variant.Price,
                    Quantity = item.Quantity
                });
            }

            model.SubTotal = model.Items.Sum(i => i.Subtotal);
            model.DeliveryFee = model.Items.Count > 0 ? DeliveryFee : 0m;
            model.Total = model.SubTotal + model.DeliveryFee;

            return model;
        }

        // =========================================================
        // PLACE ORDER (TRANSACTIONAL)
        // =========================================================
        public async Task<OrderPlacementResult> PlaceOrderAsync(Guid customerId, CheckoutViewModel model)
        {
            var result = new OrderPlacementResult();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
            {
                result.Errors.Add("Customer profile was not found.");
                return result;
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null || !cart.Items.Any())
            {
                result.Errors.Add("Your cart is empty.");
                return result;
            }

            // Validate Payment Method
            if (!model.PaymentMethodId.HasValue)
            {
                result.Errors.Add("Please select a payment method.");
                return result;
            }

            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(x => x.Id == model.PaymentMethodId.Value && x.IsActive);

            if (paymentMethod == null)
            {
                result.Errors.Add("The selected payment method is not active or available.");
                return result;
            }

            // Validate Stock for all cart items
            foreach (var item in cart.Items)
            {
                if (item.Variant == null)
                {
                    result.Errors.Add("An item in your cart is no longer available.");
                    continue;
                }

                var available = item.Variant.Inventory?.AvailableQuantity ?? 0;
                if (available < item.Quantity)
                {
                    result.Errors.Add($"Not enough stock for '{item.Variant.Product?.Name ?? "Product"}' (SKU: {item.Variant.SKU}). Available: {available}, Requested: {item.Quantity}.");
                }
            }

            if (result.Errors.Any())
            {
                return result;
            }

            // Begin Atomic Transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Resolve or Create Address
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a =>
                        a.CustomerId == customerId &&
                        a.Street == model.Street &&
                        a.City == model.City &&
                        a.Province == model.Province);

                if (address == null)
                {
                    var hasExisting = await _context.Addresses.AnyAsync(a => a.CustomerId == customerId);
                    address = new Address
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customerId,
                        Province = model.Province.Trim(),
                        City = model.City.Trim(),
                        Street = model.Street.Trim(),
                        PostalCode = model.PostalCode?.Trim() ?? string.Empty,
                        IsDefault = !hasExisting
                    };
                    _context.Addresses.Add(address);
                }
                else if (!string.IsNullOrWhiteSpace(model.PostalCode))
                {
                    address.PostalCode = model.PostalCode.Trim();
                }

                // 2. Update Customer Profile info
                customer.FirstName = model.FirstName.Trim();
                customer.LastName = model.LastName.Trim();
                customer.Phone = model.Phone?.Trim();
                customer.Email = model.Email.Trim();

                // 3. Calculate Totals
                var subtotal = cart.Items
                    .Where(i => i.Variant != null)
                    .Sum(i => i.Quantity * i.Variant!.Price);

                var deliveryFee = subtotal > 0 ? DeliveryFee : 0m;
                var total = subtotal + deliveryFee;

                // 4. Resolve Payment & Order Status
                var isKhqrOrAba = paymentMethod.Name.Contains("KHQR", StringComparison.OrdinalIgnoreCase)
                               || paymentMethod.Name.Contains("ABA", StringComparison.OrdinalIgnoreCase);

                var isPaidOnline = model.IsPaymentConfirmed && isKhqrOrAba;
                var paymentStatus = isPaidOnline ? PaymentStatus.Paid : PaymentStatus.Pending;
                var orderStatus = isPaidOnline ? OrderStatus.Confirmed : OrderStatus.Pending;

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    AddressId = address.Id,
                    OrderDate = DateTime.UtcNow,
                    Status = orderStatus,
                    TotalAmount = total
                };
                _context.Orders.Add(order);

                // 5. Create Order Items & Reserve/Deduct Inventory
                foreach (var cartItem in cart.Items)
                {
                    if (cartItem.Variant == null) continue;

                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        VariantId = cartItem.VariantId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Variant.Price
                    };
                    _context.OrderItems.Add(orderItem);

                    // Update inventory reserved/available
                    if (cartItem.Variant.Inventory != null)
                    {
                        cartItem.Variant.Inventory.ReservedQuantity += cartItem.Quantity;
                        cartItem.Variant.Inventory.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // 6. Create Payment Record
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PaymentMethodId = paymentMethod.Id,
                    PaymentStatus = paymentStatus,
                    Amount = total,
                    PaidAt = paymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : null
                };
                _context.Payments.Add(payment);

                // 7. Create Shipment Record
                var shipment = new Shipment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ShippingCompany = "Standard Courier",
                    TrackingNumber = string.Empty,
                    ShipmentStatus = ShipmentStatus.Pending,
                    ShippedAt = null,
                    DeliveredAt = null
                };
                _context.Shipments.Add(shipment);

                // 8. Clear Cart
                _context.CartItems.RemoveRange(cart.Items);

                // Commit Transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = true;
                result.OrderId = order.Id;
                result.Message = "Your order has been placed successfully!";
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Success = false;
                result.Errors.Add("An error occurred while placing your order. Please try again: " + ex.Message);
                return result;
            }
        }

        // =========================================================
        // GET CUSTOMER ORDERS
        // =========================================================
        public async Task<CustomerOrdersResult> GetCustomerOrdersAsync(
            Guid customerId,
            OrderStatus? status,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var baseQuery = _context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId);

            var allCount = await baseQuery.CountAsync();
            var pendingCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Pending);
            var processingCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Processing);
            var shippedCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Shipped);
            var deliveredCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Delivered);
            var cancelledCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Cancelled);

            var query = baseQuery;
            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)
                .Include(o => o.Payment)
                .Include(o => o.Shipment)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new CustomerOrdersResult
            {
                Orders = orders,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                CurrentStatus = status,
                AllCount = allCount,
                PendingCount = pendingCount,
                ProcessingCount = processingCount,
                ShippedCount = shippedCount,
                DeliveredCount = deliveredCount,
                CancelledCount = cancelledCount
            };
        }

        // =========================================================
        // GET CUSTOMER ORDER BY ID
        // =========================================================
        public async Task<Order?> GetCustomerOrderByIdAsync(Guid customerId, Guid orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Address)
                .Include(o => o.Payment)
                    .ThenInclude(p => p!.PaymentMethod)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);
        }

        // =========================================================
        // CANCEL CUSTOMER ORDER
        // =========================================================
        public async Task<(bool Success, string Message)> CancelCustomerOrderAsync(Guid customerId, Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

            if (order == null)
            {
                return (false, "Order was not found.");
            }

            if (order.Status != OrderStatus.Pending)
            {
                return (false, "Only orders with 'Pending' status can be cancelled.");
            }

            order.Status = OrderStatus.Cancelled;

            // Release reserved inventory
            foreach (var item in order.Items)
            {
                if (item.Variant?.Inventory != null)
                {
                    item.Variant.Inventory.ReservedQuantity = Math.Max(0, item.Variant.Inventory.ReservedQuantity - item.Quantity);
                    item.Variant.Inventory.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return (true, "Your order has been cancelled successfully.");
        }

        // =========================================================
        // ADMIN: GET ORDERS
        // =========================================================
        public async Task<List<Order>> GetAdminOrdersAsync(string? search = null, OrderStatus? status = null)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.Payment)
                .Include(o => o.Shipment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    (o.Customer != null && (
                        o.Customer.FirstName.Contains(search) ||
                        o.Customer.LastName.Contains(search) ||
                        o.Customer.Email.Contains(search)
                    ))
                );
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            return await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // =========================================================
        // ADMIN: GET ORDER DETAILS
        // =========================================================
        public async Task<Order?> GetAdminOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.Payment)
                    .ThenInclude(p => p!.PaymentMethod)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)
                .FirstOrDefaultAsync(m => m.Id == orderId);
        }

        // =========================================================
        // ADMIN: UPDATE ORDER STATUS
        // =========================================================
        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            var prevStatus = order.Status;
            order.Status = status;

            // If moving to Completed/Delivered, decrement total quantity and release reserved
            if ((status == OrderStatus.Delivered || status == OrderStatus.Processing) && prevStatus == OrderStatus.Pending)
            {
                foreach (var item in order.Items)
                {
                    if (item.Variant?.Inventory != null)
                    {
                        item.Variant.Inventory.Quantity = Math.Max(0, item.Variant.Inventory.Quantity - item.Quantity);
                        item.Variant.Inventory.ReservedQuantity = Math.Max(0, item.Variant.Inventory.ReservedQuantity - item.Quantity);
                        item.Variant.Inventory.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
            // If moving to Cancelled, release reserved quantity
            else if (status == OrderStatus.Cancelled && prevStatus != OrderStatus.Cancelled)
            {
                foreach (var item in order.Items)
                {
                    if (item.Variant?.Inventory != null)
                    {
                        item.Variant.Inventory.ReservedQuantity = Math.Max(0, item.Variant.Inventory.ReservedQuantity - item.Quantity);
                        item.Variant.Inventory.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // ADMIN: DELETE ORDER
        // =========================================================
        public async Task<bool> DeleteOrderAsync(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Payment)
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            if (order.Payment != null) _context.Payments.Remove(order.Payment);
            if (order.Shipment != null) _context.Shipments.Remove(order.Shipment);
            _context.OrderItems.RemoveRange(order.Items);
            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(List<Customer> Customers, List<Address> Addresses)> GetOrderCreateDropdownDataAsync()
        {
            var customers = await _context.Customers.AsNoTracking().ToListAsync();
            var addresses = await _context.Addresses.AsNoTracking().ToListAsync();
            return (customers, addresses);
        }

        public async Task<ServiceResult> CreateAdminOrderAsync(Order order)
        {
            order.Id = Guid.NewGuid();
            order.OrderDate = DateTime.UtcNow;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Order created successfully.");
        }

        public async Task<List<OrderItem>> GetAllOrderItemsAsync()
        {
            return await _context.OrderItems.AsNoTracking().ToListAsync();
        }

        public async Task<OrderItem?> GetOrderItemByIdAsync(Guid id)
        {
            return await _context.OrderItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ServiceResult> CreateOrderItemAsync(OrderItem item)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }
            _context.OrderItems.Add(item);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Order item created successfully.");
        }

        public async Task<ServiceResult> UpdateOrderItemAsync(Guid id, OrderItem item)
        {
            var existing = await _context.OrderItems.FirstOrDefaultAsync(o => o.Id == id);
            if (existing == null)
            {
                return ServiceResult.Fail("Order item not found.");
            }

            existing.OrderId = item.OrderId;
            existing.VariantId = item.VariantId;
            existing.Quantity = item.Quantity;
            existing.UnitPrice = item.UnitPrice;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Order item updated successfully.");
        }

        public async Task<ServiceResult> DeleteOrderItemAsync(Guid id)
        {
            var item = await _context.OrderItems.FindAsync(id);
            if (item == null)
            {
                return ServiceResult.Fail("Order item not found.");
            }

            _context.OrderItems.Remove(item);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Order item deleted successfully.");
        }
    }
}
