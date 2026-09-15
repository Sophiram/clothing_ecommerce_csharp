using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        #region DbSets

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<Size> Sizes => Set<Size>();
        public DbSet<Color> Colors => Set<Color>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<Wishlist> Wishlists => Set<Wishlist>();
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
        public DbSet<Review> Reviews => Set<Review>();


        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===========================
            // Customer
            // ===========================

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();

            // ===========================
            // Address
            // ===========================

            modelBuilder.Entity<Address>()
                .HasOne(a => a.Customer)
                .WithMany(c => c.Addresses)
                .HasForeignKey(a => a.CustomerId);


            // ===========================
            // Customer <-> ApplicationUser (1:1)
            // ===========================

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.ApplicationUserId)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.ApplicationUser)
                .WithOne()
                .HasForeignKey<Customer>(c => c.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===========================
            // Category
            // ===========================

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // ===========================
            // Brand
            // ===========================

            modelBuilder.Entity<Brand>()
                .HasIndex(b => b.Name)
                .IsUnique();

            // ===========================
            // Product
            // ===========================

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId);

            // ===========================
            // Product Image
            // ===========================

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===========================
            // Size
            // ===========================

            modelBuilder.Entity<Size>()
                .HasIndex(s => s.Name)
                .IsUnique();

            // ===========================
            // Color
            // ===========================

            modelBuilder.Entity<Color>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // ===========================
            // Product Variant
            // ===========================

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(v => v.SKU)
                .IsUnique();

            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Size)
                .WithMany(s => s.Variants)
                .HasForeignKey(v => v.SizeId);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Color)
                .WithMany(c => c.Variants)
                .HasForeignKey(v => v.ColorId);

            // ===========================
            // Inventory (1:1)
            // ===========================

            modelBuilder.Entity<Inventory>()
                .HasIndex(i => i.ProductVariantId)
                .IsUnique();

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.ProductVariant)
                .WithOne(v => v.Inventory)
                .HasForeignKey<Inventory>(i => i.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===========================
            // Cart
            // ===========================

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithOne(c => c.Cart)
                .HasForeignKey<Cart>(c => c.CustomerId);

            // ===========================
            // Cart Item
            // ===========================

            modelBuilder.Entity<CartItem>()
                .HasOne(i => i.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CartId);

            modelBuilder.Entity<CartItem>()
                .HasOne(i => i.Variant)
                .WithMany()
                .HasForeignKey(i => i.VariantId);

            // ===========================
            // Order
            // ===========================

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Address)
                .WithMany()
                .HasForeignKey(o => o.AddressId)
                .OnDelete(DeleteBehavior.NoAction);

            // ===========================
            // Order Item
            // ===========================

            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Variant)
                .WithMany()
                .HasForeignKey(i => i.VariantId);

            // ===========================
            // Payment (1:1 Order)
            // PaymentMethod (Many Payments -> One PaymentMethod)
            // ===========================

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.OrderId)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.PaymentMethod)
                .WithMany(pm => pm.Payments)
                .HasForeignKey(p => p.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentMethod>()
                .HasIndex(pm => pm.Name)
                .IsUnique();

            modelBuilder.Entity<PaymentMethod>()
                .Property(pm => pm.Name)
                .HasMaxLength(100);

            modelBuilder.Entity<PaymentMethod>()
                .Property(pm => pm.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<PaymentMethod>()
                .Property(pm => pm.Icon)
                .HasMaxLength(500);

            // ===========================
            // Shipment (1:1)
            // ===========================

            modelBuilder.Entity<Shipment>()
                .HasIndex(s => s.OrderId)
                .IsUnique();

            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.Order)
                .WithOne(o => o.Shipment)
                .HasForeignKey<Shipment>(s => s.OrderId);

            // ===========================
            // Wishlist
            // ===========================

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Customer)
                .WithOne(c => c.Wishlist)
                .HasForeignKey<Wishlist>(w => w.CustomerId);

            // ===========================
            // Wishlist Item
            // ===========================

            modelBuilder.Entity<WishlistItem>()
                .HasOne(i => i.Wishlist)
                .WithMany(w => w.Items)
                .HasForeignKey(i => i.WishlistId);

            modelBuilder.Entity<WishlistItem>()
                .HasOne(i => i.Variant)
                .WithMany()
                .HasForeignKey(i => i.VariantId);

            // ===========================
            // Review
            // ===========================

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId);

            modelBuilder.Entity<Review>()
     .HasOne(r => r.Product)
     .WithMany(p => p.Reviews)
     .HasForeignKey(r => r.ProductId);

            // ===========================
            // Decimal Precision
            // ===========================

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Price)
                .HasPrecision(18, 2);

        } // End OnModelCreating

    } // End AppDbContext

} // End namespace