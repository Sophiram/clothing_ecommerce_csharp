namespace ClothingEcommerce.Shared.DTOs.Wishlist
{
    public class WishlistItemDto
    {
        public Guid Id { get; set; }
        public Guid VariantId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public string? SizeName { get; set; }
        public string? ColorName { get; set; }
        public string? ColorHex { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class WishlistDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public List<WishlistItemDto> Items { get; set; } = new();
        public int TotalItems => Items.Count;
    }

    public class WishlistActionRequestDto
    {
        public Guid VariantId { get; set; }
    }

    public class WishlistRemoveRequestDto
    {
        public Guid? Id { get; set; }
        public Guid? VariantId { get; set; }
    }

    public class WishlistMoveRequestDto
    {
        public Guid WishlistItemId { get; set; }
    }
}
