using System.ComponentModel.DataAnnotations;

namespace ClothingEcommerce.Shared.DTOs.Orders
{
    public class CartItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public Guid? VariantId { get; set; }
        public string? VariantSku { get; set; }
        public string? SizeName { get; set; }
        public string? ColorName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public int MaxStock { get; set; }
    }

    public class CartDto
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal Subtotal => Items.Sum(i => i.TotalPrice);
        public decimal ShippingFee { get; set; }
        public decimal Total => Subtotal + ShippingFee;
    }

    public class AddToCartRequestDto
    {
        [Required]
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemRequestDto
    {
        [Required]
        public Guid CartItemId { get; set; }
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; }
    }

    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public string? SizeName { get; set; }
        public string? ColorName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class OrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "KHQR";
        public DateTime CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class CheckoutRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping address is required.")]
        public string Address { get; set; } = string.Empty;

        public string? City { get; set; } = "Phnom Penh";
        public string? PostalCode { get; set; }
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        public string PaymentMethod { get; set; } = "KHQR"; // KHQR, COD, ABA_PAY, etc.
    }
}
