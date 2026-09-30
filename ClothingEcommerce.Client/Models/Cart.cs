namespace WebApplication_ClothingEcommerce.Models
{
    public class Cart
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public DateTime CreatedAt { get; set; }

        public Customer Customer { get; set; } = null!;

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
