namespace WebApplication_ClothingEcommerce.Models
{
    public class Wishlist
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;

        public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
    }
}
