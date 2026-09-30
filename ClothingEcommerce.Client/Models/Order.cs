namespace WebApplication_ClothingEcommerce.Models
{
    using WebApplication_ClothingEcommerce.Data.Enums;

    public class Order
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public Guid AddressId { get; set; }

        public DateTime OrderDate { get; set; }

        public OrderStatus Status { get; set; }

        public decimal TotalAmount { get; set; }

        public Customer Customer { get; set; } = null!;

        public Address Address { get; set; } = null!;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        public Payment? Payment { get; set; }

        public Shipment? Shipment { get; set; }
    }
}
