using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace ClothingEcommerce.Client.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IApiClient _apiClient;

        public OrdersController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private Order MapDtoToOrder(OrderDto dto)
        {
            var status = Enum.TryParse<OrderStatus>(dto.Status, true, out var os) ? os : OrderStatus.Pending;
            var paymentStatus = Enum.TryParse<PaymentStatus>(dto.PaymentStatus, true, out var ps) ? ps : PaymentStatus.Pending;

            return new Order
            {
                Id = dto.Id,
                OrderDate = dto.CreatedAt,
                TotalAmount = dto.TotalAmount,
                Status = status,
                Customer = new Customer
                {
                    FirstName = dto.CustomerName.Split(' ').FirstOrDefault() ?? dto.CustomerName,
                    LastName = dto.CustomerName.Contains(' ') ? dto.CustomerName.Substring(dto.CustomerName.IndexOf(' ') + 1) : "",
                    Email = dto.CustomerEmail,
                    Phone = dto.CustomerPhone
                },
                Address = new Address
                {
                    Street = dto.ShippingAddress,
                    City = dto.City ?? "Phnom Penh",
                    PostalCode = dto.PostalCode ?? "12000"
                },
                Payment = new Payment
                {
                    PaymentStatus = paymentStatus,
                    PaymentMethod = new PaymentMethod { Name = dto.PaymentMethod },
                    BakongTransactionId = dto.OrderNumber,
                    CreatedAt = dto.CreatedAt
                },
                Shipment = new Shipment
                {
                    ShipmentStatus = status == OrderStatus.Delivered ? ShipmentStatus.Delivered : (status == OrderStatus.Shipped ? ShipmentStatus.Shipped : ShipmentStatus.Pending),
                    ShippingCompany = "VET Express"
                },
                Items = dto.Items.Select(oi => new OrderItem
                {
                    Id = oi.Id,
                    OrderId = dto.Id,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    Variant = new ProductVariant
                    {
                        ProductId = oi.ProductId,
                        Price = oi.UnitPrice,
                        Product = new Product
                        {
                            Id = oi.ProductId,
                            Name = oi.ProductName,
                            Images = !string.IsNullOrEmpty(oi.ProductImage)
                                ? new List<ProductImage> { new() { ImageUrl = oi.ProductImage, IsPrimary = true } }
                                : new List<ProductImage>()
                        },
                        Size = !string.IsNullOrEmpty(oi.SizeName) ? new Size { Name = oi.SizeName } : null,
                        Color = !string.IsNullOrEmpty(oi.ColorName) ? new Color { Name = oi.ColorName } : null
                    }
                }).ToList()
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? status = null, int page = 1)
        {
            var response = await _apiClient.GetAsync<List<OrderDto>>("api/orders");
            var dtos = response?.Data ?? new List<OrderDto>();

            var allOrders = dtos.Select(MapDtoToOrder).ToList();

            ViewBag.AllCount = allOrders.Count;
            ViewBag.PendingCount = allOrders.Count(o => o.Status == OrderStatus.Pending);
            ViewBag.ProcessingCount = allOrders.Count(o => o.Status == OrderStatus.Processing);
            ViewBag.ShippedCount = allOrders.Count(o => o.Status == OrderStatus.Shipped);
            ViewBag.DeliveredCount = allOrders.Count(o => o.Status == OrderStatus.Delivered);
            ViewBag.CancelledCount = allOrders.Count(o => o.Status == OrderStatus.Cancelled);

            var query = allOrders.AsEnumerable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(o => o.Status == parsedStatus);
                ViewBag.CurrentStatus = parsedStatus;
            }
            else
            {
                ViewBag.CurrentStatus = null;
            }

            var pageSize = 10;
            var totalOrders = query.Count();
            var totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
            if (totalPages < 1) totalPages = 1;

            var pagedOrders = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedOrders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var response = await _apiClient.GetAsync<OrderDto>($"api/orders/{id}");
            if (response == null || !response.Success || response.Data == null)
            {
                return NotFound();
            }

            var order = MapDtoToOrder(response.Data);
            return View(order);
        }
    }
}
