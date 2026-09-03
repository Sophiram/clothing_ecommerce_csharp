using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Data.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace WebApplication_ClothingEcommerce.Data
{
    public static class AppDbInitializer
    {
        public static async Task Seed(IApplicationBuilder applicationBuilder)
        {
            using var serviceScope = applicationBuilder.ApplicationServices.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Run Migrations automatically
            await context.Database.MigrateAsync();

            // ==========================================
            // 1. SEED ROLES & USERS (IDENTITY)
            // ==========================================
            var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new IdentityRole("User"));

            var adminEmail = "admin@clothing.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    FirstName = "Admin",
                    LastName = "System",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(newAdmin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }

            // បង្កើត Default Customer User សម្រាប់តេស្ត
            var customerEmail = "john.doe@gmail.com";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);
            if (customerUser == null)
            {
                var newCustomerUser = new ApplicationUser
                {
                    FirstName = "John",
                    LastName = "Doe",
                    UserName = customerEmail,
                    Email = customerEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(newCustomerUser, "User@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newCustomerUser, "User");
                }
                customerUser = newCustomerUser;
            }

            // ==========================================
            // 2. CATEGORY
            // ==========================================
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Id = Guid.NewGuid(), Name = "T-Shirts", Description = "Casual and comfortable T-Shirts" },
                    new Category { Id = Guid.NewGuid(), Name = "Shirts", Description = "Formal and casual shirts" },
                    new Category { Id = Guid.NewGuid(), Name = "Pants", Description = "Jeans, trousers and casual pants" },
                    new Category { Id = Guid.NewGuid(), Name = "Jackets", Description = "Jackets and outerwear" },
                    new Category { Id = Guid.NewGuid(), Name = "Dresses", Description = "Women's dresses" }
                );
                await context.SaveChangesAsync();
            }

            // ==========================================
            // 3. BRAND
            // ==========================================
            if (!context.Brands.Any())
            {
                context.Brands.AddRange(
                    new Brand { Id = Guid.NewGuid(), Name = "Nike", Description = "Sportswear and lifestyle brand" },
                    new Brand { Id = Guid.NewGuid(), Name = "Adidas", Description = "Sports and lifestyle clothing brand" },
                    new Brand { Id = Guid.NewGuid(), Name = "Puma", Description = "Sportswear and footwear brand" },
                    new Brand { Id = Guid.NewGuid(), Name = "Uniqlo", Description = "Japanese casual wear brand" }
                );
                await context.SaveChangesAsync();
            }

            // ==========================================
            // 4. SIZE
            // ==========================================
            if (!context.Sizes.Any())
            {
                context.Sizes.AddRange(
                    new Size { Id = Guid.NewGuid(), Name = "S" },
                    new Size { Id = Guid.NewGuid(), Name = "M" },
                    new Size { Id = Guid.NewGuid(), Name = "L" },
                    new Size { Id = Guid.NewGuid(), Name = "XL" }
                );
                await context.SaveChangesAsync();
            }

            // ==========================================
            // 5. COLOR
            // ==========================================
            if (!context.Colors.Any())
            {
                context.Colors.AddRange(
                    new Color { Id = Guid.NewGuid(), Name = "Black", HexCode = "#000000" },
                    new Color { Id = Guid.NewGuid(), Name = "White", HexCode = "#FFFFFF" },
                    new Color { Id = Guid.NewGuid(), Name = "Red", HexCode = "#FF0000" },
                    new Color { Id = Guid.NewGuid(), Name = "Blue", HexCode = "#0000FF" }
                );
                await context.SaveChangesAsync();
            }

            // ==========================================
            // 6. PRODUCT
            // ==========================================
            if (!context.Products.Any())
            {
                var tShirts = context.Categories.First(c => c.Name == "T-Shirts");
                var shirts = context.Categories.First(c => c.Name == "Shirts");
                var pants = context.Categories.First(c => c.Name == "Pants");
                var jackets = context.Categories.First(c => c.Name == "Jackets");
                var dresses = context.Categories.First(c => c.Name == "Dresses");

                var nike = context.Brands.First(b => b.Name == "Nike");
                var adidas = context.Brands.First(b => b.Name == "Adidas");
                var puma = context.Brands.First(b => b.Name == "Puma");
                var uniqlo = context.Brands.First(b => b.Name == "Uniqlo");

                var now = DateTime.UtcNow;

                context.Products.AddRange(
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = tShirts.Id,
                        BrandId = nike.Id,
                        Name = "Nike Sports T-Shirt",
                        Description = "Comfortable Nike sports T-Shirt made from soft cotton.",
                        Gender = "Men",
                        Material = "Cotton",
                        Status = ProductStatus.Active,
                        CreatedAt = now,
                        ModifiedAt = now
                    },
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = tShirts.Id,
                        BrandId = adidas.Id,
                        Name = "Adidas Classic T-Shirt",
                        Description = "Classic Adidas cotton T-Shirt for everyday wear.",
                        Gender = "Unisex",
                        Material = "Cotton",
                        Status = ProductStatus.Active,
                        CreatedAt = now,
                        ModifiedAt = now
                    },
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = shirts.Id,
                        BrandId = uniqlo.Id,
                        Name = "Uniqlo Casual Shirt",
                        Description = "Simple and comfortable casual shirt.",
                        Gender = "Men",
                        Material = "Cotton",
                        Status = ProductStatus.Active,
                        CreatedAt = now,
                        ModifiedAt = now
                    },
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = pants.Id,
                        BrandId = puma.Id,
                        Name = "Puma Casual Pants",
                        Description = "Comfortable casual pants for everyday activities.",
                        Gender = "Men",
                        Material = "Polyester",
                        Status = ProductStatus.Active,
                        CreatedAt = now,
                        ModifiedAt = now
                    },
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = jackets.Id,
                        BrandId = nike.Id,
                        Name = "Nike Sports Jacket",
                        Description = "Lightweight sports jacket for outdoor activities.",
                        Gender = "Unisex",
                        Material = "Polyester",
                        Status = ProductStatus.Active,
                        CreatedAt = now,
                        ModifiedAt = now
                    },
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = dresses.Id,
                        BrandId = uniqlo.Id,
                        Name = "Uniqlo Summer Dress",
                        Description = "Comfortable cotton summer dress with a modern style.",
                        Gender = "Women",
                        Material = "Cotton",
                        Status = ProductStatus.Active,
                        CreatedAt = now,
                        ModifiedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ==========================================
            // 7. PRODUCT IMAGES
            // ==========================================
            if (!context.ProductImages.Any())
            {
                var products = context.Products.ToList();

                var nikeTshirt = products.First(p => p.Name == "Nike Sports T-Shirt");
                var adidasTshirt = products.First(p => p.Name == "Adidas Classic T-Shirt");
                var casualShirt = products.First(p => p.Name == "Uniqlo Casual Shirt");
                var casualPants = products.First(p => p.Name == "Puma Casual Pants");
                var sportsJacket = products.First(p => p.Name == "Nike Sports Jacket");
                var summerDress = products.First(p => p.Name == "Uniqlo Summer Dress");

                context.ProductImages.AddRange(
                    new ProductImage { Id = Guid.NewGuid(), ProductId = nikeTshirt.Id, ImageUrl = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab", IsPrimary = true },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = nikeTshirt.Id, ImageUrl = "https://images.unsplash.com/photo-1503341504253-dff4815485f1", IsPrimary = false },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = adidasTshirt.Id, ImageUrl = "https://m.media-amazon.com/images/I/61vFNACIHiL._AC_UL480_FMwebp_QL65_.jpg", IsPrimary = true },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = adidasTshirt.Id, ImageUrl = "https://images.unsplash.com/photo-1523398002811-999ca8dec234", IsPrimary = false },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = casualShirt.Id, ImageUrl = "https://images.unsplash.com/photo-1596755389378-c31d21fd1273", IsPrimary = true },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = casualShirt.Id, ImageUrl = "https://images.unsplash.com/photo-1603252110481-7ba873bf42ab", IsPrimary = false },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = casualPants.Id, ImageUrl = "https://images.unsplash.com/photo-1624378439575-d8705ad7ae80", IsPrimary = true },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = casualPants.Id, ImageUrl = "https://images.unsplash.com/photo-1473966968600-fa801b869a1a", IsPrimary = false },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = sportsJacket.Id, ImageUrl = "https://images.unsplash.com/photo-1551028719-00167b16eac5", IsPrimary = true },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = sportsJacket.Id, ImageUrl = "https://images.unsplash.com/photo-1544022613-e87ca75a784a", IsPrimary = false },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = summerDress.Id, ImageUrl = "https://images.unsplash.com/photo-1595777457583-95e059d581b8", IsPrimary = true },
                    new ProductImage { Id = Guid.NewGuid(), ProductId = summerDress.Id, ImageUrl = "https://images.unsplash.com/photo-1515372039744-b8f02a3ae446", IsPrimary = false }
                );

                await context.SaveChangesAsync();
            }

            // ==========================================
            // 8. PRODUCT VARIANTS
            // ==========================================
            if (!context.ProductVariants.Any())
            {
                var products = context.Products.ToList();

                var small = context.Sizes.First(s => s.Name == "S");
                var medium = context.Sizes.First(s => s.Name == "M");
                var large = context.Sizes.First(s => s.Name == "L");
                var xl = context.Sizes.First(s => s.Name == "XL");

                var black = context.Colors.First(c => c.Name == "Black");
                var white = context.Colors.First(c => c.Name == "White");
                var red = context.Colors.First(c => c.Name == "Red");
                var blue = context.Colors.First(c => c.Name == "Blue");

                var nikeTshirt = products.First(p => p.Name == "Nike Sports T-Shirt");
                var adidasTshirt = products.First(p => p.Name == "Adidas Classic T-Shirt");
                var casualShirt = products.First(p => p.Name == "Uniqlo Casual Shirt");
                var casualPants = products.First(p => p.Name == "Puma Casual Pants");
                var sportsJacket = products.First(p => p.Name == "Nike Sports Jacket");
                var summerDress = products.First(p => p.Name == "Uniqlo Summer Dress");

                context.ProductVariants.AddRange(
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = nikeTshirt.Id, SizeId = small.Id, ColorId = black.Id, SKU = "NIKE-TS-BLK-S", Status = VariantStatus.Available, Price = 29.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = nikeTshirt.Id, SizeId = medium.Id, ColorId = black.Id, SKU = "NIKE-TS-BLK-M", Status = VariantStatus.Available, Price = 29.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = nikeTshirt.Id, SizeId = large.Id, ColorId = white.Id, SKU = "NIKE-TS-WHT-L", Status = VariantStatus.Available, Price = 29.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = nikeTshirt.Id, SizeId = xl.Id, ColorId = red.Id, SKU = "NIKE-TS-RED-XL", Status = VariantStatus.Available, Price = 31.99m },

                    new ProductVariant { Id = Guid.NewGuid(), ProductId = adidasTshirt.Id, SizeId = small.Id, ColorId = white.Id, SKU = "ADI-TS-WHT-S", Status = VariantStatus.Available, Price = 27.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = adidasTshirt.Id, SizeId = medium.Id, ColorId = red.Id, SKU = "ADI-TS-RED-M", Status = VariantStatus.Available, Price = 27.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = adidasTshirt.Id, SizeId = large.Id, ColorId = black.Id, SKU = "ADI-TS-BLK-L", Status = VariantStatus.Available, Price = 27.99m },

                    new ProductVariant { Id = Guid.NewGuid(), ProductId = casualShirt.Id, SizeId = medium.Id, ColorId = blue.Id, SKU = "UNIQLO-SH-BLU-M", Status = VariantStatus.Available, Price = 34.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = casualShirt.Id, SizeId = large.Id, ColorId = blue.Id, SKU = "UNIQLO-SH-BLU-L", Status = VariantStatus.Available, Price = 34.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = casualShirt.Id, SizeId = xl.Id, ColorId = white.Id, SKU = "UNIQLO-SH-WHT-XL", Status = VariantStatus.Available, Price = 36.99m },

                    new ProductVariant { Id = Guid.NewGuid(), ProductId = casualPants.Id, SizeId = medium.Id, ColorId = black.Id, SKU = "PUMA-PANTS-BLK-M", Status = VariantStatus.Available, Price = 49.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = casualPants.Id, SizeId = large.Id, ColorId = black.Id, SKU = "PUMA-PANTS-BLK-L", Status = VariantStatus.Available, Price = 49.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = casualPants.Id, SizeId = xl.Id, ColorId = blue.Id, SKU = "PUMA-PANTS-BLU-XL", Status = VariantStatus.Available, Price = 52.99m },

                    new ProductVariant { Id = Guid.NewGuid(), ProductId = sportsJacket.Id, SizeId = medium.Id, ColorId = black.Id, SKU = "NIKE-JACKET-BLK-M", Status = VariantStatus.Available, Price = 79.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = sportsJacket.Id, SizeId = large.Id, ColorId = black.Id, SKU = "NIKE-JACKET-BLK-L", Status = VariantStatus.Available, Price = 79.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = sportsJacket.Id, SizeId = xl.Id, ColorId = red.Id, SKU = "NIKE-JACKET-RED-XL", Status = VariantStatus.Available, Price = 84.99m },

                    new ProductVariant { Id = Guid.NewGuid(), ProductId = summerDress.Id, SizeId = small.Id, ColorId = white.Id, SKU = "UNIQLO-DRESS-WHT-S", Status = VariantStatus.Available, Price = 59.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = summerDress.Id, SizeId = medium.Id, ColorId = white.Id, SKU = "UNIQLO-DRESS-WHT-M", Status = VariantStatus.Available, Price = 59.99m },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = summerDress.Id, SizeId = large.Id, ColorId = red.Id, SKU = "UNIQLO-DRESS-RED-L", Status = VariantStatus.Available, Price = 62.99m }
                );

                await context.SaveChangesAsync();
            }

            // ==========================================
            // 9. INVENTORIES
            // ==========================================
            if (!context.Inventories.Any())
            {
                var variants = context.ProductVariants.ToList();

                foreach (var variant in variants)
                {
                    context.Inventories.Add(new Inventory
                    {
                        Id = Guid.NewGuid(),
                        ProductVariantId = variant.Id,
                        Quantity = 100,
                        ReservedQuantity = 0,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();
            }

            // ==========================================
            // 10. CUSTOMERS & ADDRESSES
            // ==========================================
            if (!context.Customers.Any())
            {
                var customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    ApplicationUserId = customerUser.Id, // ភ្ជាប់ Identity User ID នៅត្រង់នេះ
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@gmail.com",
                    Phone = "012345678",
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                context.Customers.Add(customer);

                context.Addresses.Add(new Address
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Street = "Street 271, Sangkat Takhmao",
                    City = "Phnom Penh",
                    PostalCode = "12000"
                });

                await context.SaveChangesAsync();
            }

            // ==========================================
            // 11. CART & CART ITEMS
            // ==========================================
            if (!context.Carts.Any())
            {
                var customer = context.Customers.First();
                var variant = context.ProductVariants.First();

                var cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Carts.Add(cart);

                context.CartItems.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    VariantId = variant.Id,
                    Quantity = 2
                });

                await context.SaveChangesAsync();
            }

            // ==========================================
            // 12. WISHLIST & WISHLIST ITEMS
            // ==========================================
            if (!context.Wishlists.Any())
            {
                var customer = context.Customers.First();
                var variant = context.ProductVariants.First();

                var wishlist = new Wishlist
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id
                };
                context.Wishlists.Add(wishlist);

                context.WishlistItems.Add(new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    WishlistId = wishlist.Id,
                    VariantId = variant.Id
                });

                await context.SaveChangesAsync();
            }

         

            // ==========================================
            // 13. PAYMENT METHODS
            // ==========================================

            if (!context.PaymentMethods.Any())
            {
                context.PaymentMethods.AddRange(

                    new PaymentMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "KHQR",
                        Description = "Pay securely using KHQR",
                        Icon = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/5a/KHQR_logo.svg/512px-KHQR_logo.svg.png",
                        IsActive = true,
                        DisplayOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },

                    new PaymentMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "ABA Pay",
                        Description = "Pay using ABA Mobile",
                        Icon = null,
                        IsActive = true,
                        DisplayOrder = 2,
                        CreatedAt = DateTime.UtcNow
                    },

                    new PaymentMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "ACLEDA",
                        Description = "Pay using ACLEDA",
                        Icon = null,
                        IsActive = true,
                        DisplayOrder = 3,
                        CreatedAt = DateTime.UtcNow
                    },

                    new PaymentMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "Wing",
                        Description = "Pay using Wing",
                        Icon = null,
                        IsActive = true,
                        DisplayOrder = 4,
                        CreatedAt = DateTime.UtcNow
                    },

                    new PaymentMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "Cash on Delivery",
                        Description = "Pay cash when your order arrives",
                        Icon = null,
                        IsActive = true,
                        DisplayOrder = 5,
                        CreatedAt = DateTime.UtcNow
                    }

                );

                await context.SaveChangesAsync();
            }


            // ==========================================
            // 14. ORDERS, ORDER ITEMS, PAYMENTS & SHIPMENTS
            // ==========================================

            if (!context.Orders.Any())
            {
                var customer = context.Customers.First();
                var address = context.Addresses.First();
                var variant = context.ProductVariants.First();

                // Get an actual PaymentMethod from database
                var paymentMethod = context.PaymentMethods
                    .OrderBy(p => p.DisplayOrder)
                    .FirstOrDefault();

                if (paymentMethod == null)
                {
                    throw new InvalidOperationException(
                        "No payment method was found.");
                }

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    AddressId = address.Id,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = (variant.Price * 2) + 3.00m,
                    Status = OrderStatus.Pending
                };

                context.Orders.Add(order);

                // ==========================================
                // ORDER ITEM
                // ==========================================

                context.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    VariantId = variant.Id,
                    Quantity = 2,
                    UnitPrice = variant.Price
                });

                // ==========================================
                // PAYMENT
                // ==========================================

                context.Payments.Add(new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,

                    // Correct FK
                    PaymentMethodId = paymentMethod.Id,

                    PaymentStatus = PaymentStatus.Completed,
                    Amount = order.TotalAmount,
                    PaidAt = DateTime.UtcNow
                });

                // ==========================================
                // SHIPMENT
                // ==========================================

                context.Shipments.Add(new Shipment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ShippingCompany = "J&T Express",
                    TrackingNumber = "JT123456789",
                    ShipmentStatus = ShipmentStatus.Pending,
                    ShippedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }

            // ==========================================
            // 14. REVIEWS
            // ==========================================
            if (!context.Reviews.Any())
            {
                var customer = context.Customers.First();
                var product = context.Products.First();

                context.Reviews.Add(new Review
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    ProductId = product.Id,
                    Rating = 5,
                    Comment = "Great material and comfortable to wear!",
                    CreatedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }
        }
    }
}