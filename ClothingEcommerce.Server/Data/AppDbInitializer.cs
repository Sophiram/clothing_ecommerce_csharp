using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                // Fallback / log migration warning
                System.Diagnostics.Debug.WriteLine($"Migration warning/notice: {ex.Message}");
                try { await context.Database.EnsureCreatedAsync(); } catch { }
            }

            // Ensure HomePageSettings table exists
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HomePageSettings')
                    BEGIN
                        CREATE TABLE [HomePageSettings] (
                            [Id] int NOT NULL IDENTITY(1,1),
                            [PromoTag] nvarchar(200) NULL,
                            [PromoText] nvarchar(500) NULL,
                            [PromoCtaText] nvarchar(200) NULL,
                            [PromoCtaUrl] nvarchar(500) NULL,
                            [HeroLabel] nvarchar(100) NULL,
                            [HeroTitle] nvarchar(200) NULL,
                            [HeroHighlightWord] nvarchar(100) NULL,
                            [HeroDescription] nvarchar(max) NULL,
                            [HeroPrimaryBtnText] nvarchar(100) NULL,
                            [HeroPrimaryBtnUrl] nvarchar(500) NULL,
                            [HeroSecondaryBtnText] nvarchar(100) NULL,
                            [HeroSecondaryBtnUrl] nvarchar(500) NULL,
                            [HeroImageUrl] nvarchar(500) NULL,
                            [FloatingCard1Title] nvarchar(100) NULL,
                            [FloatingCard1Sub] nvarchar(100) NULL,
                            [FloatingCard2Title] nvarchar(100) NULL,
                            [FloatingCard2Sub] nvarchar(100) NULL,
                            [Stat1Value] nvarchar(50) NULL,
                            [Stat1Label] nvarchar(50) NULL,
                            [Stat2Value] nvarchar(50) NULL,
                            [Stat2Label] nvarchar(50) NULL,
                            [Stat3Value] nvarchar(50) NULL,
                            [Stat3Label] nvarchar(50) NULL,
                            CONSTRAINT [PK_HomePageSettings] PRIMARY KEY ([Id])
                        );
                    END
                ");

                // Ensure StoreReceiptSettings table exists
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StoreReceiptSettings')
                    BEGIN
                        CREATE TABLE [StoreReceiptSettings] (
                            [Id] int NOT NULL IDENTITY(1,1),
                            [StoreName] nvarchar(100) NOT NULL DEFAULT 'CLOTHÉ',
                            [StoreNameKh] nvarchar(150) NULL,
                            [Tagline] nvarchar(200) NULL,
                            [TaglineKh] nvarchar(250) NULL,
                            [Address] nvarchar(300) NULL,
                            [AddressKh] nvarchar(350) NULL,
                            [Phone] nvarchar(50) NULL,
                            [Email] nvarchar(100) NULL,
                            [Website] nvarchar(200) NULL,
                            [Telegram] nvarchar(100) NULL,
                            [VatNumber] nvarchar(50) NULL,
                            [ReceiptPrefix] nvarchar(20) NULL,
                            [DefaultReceiptTheme] nvarchar(50) NULL,
                            [ReturnPolicyEn] nvarchar(500) NULL,
                            [ReturnPolicyKh] nvarchar(600) NULL,
                            [ThankYouNoteEn] nvarchar(300) NULL,
                            [ThankYouNoteKh] nvarchar(400) NULL,
                            [ShowDualCurrency] bit NOT NULL DEFAULT 1,
                            [ShowKhqr] bit NOT NULL DEFAULT 1,
                            [ExchangeRate] decimal(18,2) NOT NULL DEFAULT 4100,
                            [UpdatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [UpdatedBy] nvarchar(100) NULL,
                            CONSTRAINT [PK_StoreReceiptSettings] PRIMARY KEY ([Id])
                        );
                    END
                ");

                // Ensure Payments table has new reconciliation columns
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'Currency')
                        ALTER TABLE [Payments] ADD [Currency] nvarchar(10) NOT NULL DEFAULT 'USD';
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'BakongTransactionId')
                        ALTER TABLE [Payments] ADD [BakongTransactionId] nvarchar(100) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'BakongReference')
                        ALTER TABLE [Payments] ADD [BakongReference] nvarchar(100) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'QRCode')
                        ALTER TABLE [Payments] ADD [QRCode] nvarchar(max) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'Md5Hash')
                        ALTER TABLE [Payments] ADD [Md5Hash] nvarchar(64) NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'CreatedAt')
                        ALTER TABLE [Payments] ADD [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME();
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'ExpiresAt')
                        ALTER TABLE [Payments] ADD [ExpiresAt] datetime2 NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'FailureReason')
                        ALTER TABLE [Payments] ADD [FailureReason] nvarchar(500) NULL;
                ");
            }
            catch { }

            // ==========================================
            // 1. SEED ROLES & USERS (IDENTITY)
            // ==========================================
            var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceScope.ServiceProvider.GetService<IConfiguration>();

            // Seed Roles: SuperAdmin, Admin, User
            string[] systemRoles = ["SuperAdmin", "Admin", "User"];
            foreach (var role in systemRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed SuperAdmin
            var superAdminEmail = configuration?["SUPERADMIN_EMAIL"]
                ?? Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL")
                ?? "superadmin@clothe.com";
            var superAdminPassword = configuration?["SUPERADMIN_PASSWORD"]
                ?? Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD");

            if (string.IsNullOrWhiteSpace(superAdminPassword))
            {
                var isDev = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
                    .Equals("Development", StringComparison.OrdinalIgnoreCase);
                if (isDev)
                {
                    superAdminPassword = "SuperAdmin@Dev2026!";
                }
            }

            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail)
                ?? await userManager.FindByNameAsync(superAdminEmail);
            if (superAdmin == null)
            {
                if (!string.IsNullOrWhiteSpace(superAdminPassword))
                {
                    superAdmin = new ApplicationUser
                    {
                        FirstName = "Super",
                        LastName = "Administrator",
                        UserName = superAdminEmail,
                        Email = superAdminEmail,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                    }
                }
            }
            else
            {
                if (!superAdmin.EmailConfirmed)
                {
                    superAdmin.EmailConfirmed = true;
                    await userManager.UpdateAsync(superAdmin);
                }

                if (!await userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
            }

            // Seed Admin: admin@clothe.com
            var clotheAdminEmail = "admin@clothe.com";
            var clotheAdmin = await userManager.FindByEmailAsync(clotheAdminEmail);
            if (clotheAdmin == null)
            {
                clotheAdmin = new ApplicationUser
                {
                    FirstName = "Admin",
                    LastName = "Clothe",
                    UserName = clotheAdminEmail,
                    Email = clotheAdminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(clotheAdmin, "Admin@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(clotheAdmin, "Admin");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(clotheAdmin, "Admin"))
                {
                    await userManager.AddToRoleAsync(clotheAdmin, "Admin");
                }
            }

            // Seed / Ensure Legacy Admin: admin@clothing.com
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
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
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
            var defaultCustomer = context.Customers.FirstOrDefault();
            var sampleVariant = context.ProductVariants.FirstOrDefault();

            if (defaultCustomer != null && sampleVariant != null)
            {
                var cart = context.Carts.Include(c => c.Items).FirstOrDefault(c => c.CustomerId == defaultCustomer.Id);
                if (cart == null)
                {
                    cart = new Cart
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = defaultCustomer.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Carts.Add(cart);
                    await context.SaveChangesAsync();
                }

                if (!context.CartItems.Any())
                {
                    context.CartItems.Add(new CartItem
                    {
                        Id = Guid.NewGuid(),
                        CartId = cart.Id,
                        VariantId = sampleVariant.Id,
                        Quantity = 2
                    });

                    var secondVariant = context.ProductVariants.Skip(1).FirstOrDefault();
                    if (secondVariant != null)
                    {
                        context.CartItems.Add(new CartItem
                        {
                            Id = Guid.NewGuid(),
                            CartId = cart.Id,
                            VariantId = secondVariant.Id,
                            Quantity = 1
                        });
                    }

                    await context.SaveChangesAsync();
                }
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
                        Icon = "/images/khqr-logo.svg",
                        IsActive = true,
                        DisplayOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },

                    new PaymentMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "ABA Pay",
                        Description = "Pay using ABA Mobile",
                        Icon = "/images/aba-logo.svg",
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

            // ==========================================
            // 15. DELIVERY METHODS & VET BRANCHES
            // ==========================================
            if (!context.DeliveryMethods.Any())
            {
                context.DeliveryMethods.AddRange(
                    new DeliveryMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "Store Pickup",
                        KhmerName = "មកយកនៅហាងផ្ទាល់",
                        Code = "StorePickup",
                        BaseFee = 0.00m,
                        EstimatedDeliveryTime = "Ready today",
                        RequiresBranchSelection = false,
                        IsActive = true,
                        DisplayOrder = 1
                    },
                    new DeliveryMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "VET Express",
                        KhmerName = "វីរៈ ប៊ុនថាំ អេចប្រេស",
                        Code = "VETExpress",
                        BaseFee = 2.00m,
                        EstimatedDeliveryTime = "1-2 Business days (24 Provinces)",
                        RequiresBranchSelection = true,
                        IsActive = true,
                        DisplayOrder = 2
                    },
                    new DeliveryMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "Phnom Penh City Delivery",
                        KhmerName = "ដឹកជញ្ជូនក្នុង រាជធានីភ្នំពេញ",
                        Code = "CityDelivery",
                        BaseFee = 1.50m,
                        EstimatedDeliveryTime = "Same Day (2-4 Hours)",
                        RequiresBranchSelection = false,
                        IsActive = true,
                        DisplayOrder = 3
                    },
                    new DeliveryMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "Other Express Carriers",
                        KhmerName = "ក្រុមហ៊ុនដឹកជញ្ជូនផ្សេងទៀត",
                        Code = "OtherExpress",
                        BaseFee = 2.00m,
                        EstimatedDeliveryTime = "1-3 Business days",
                        RequiresBranchSelection = false,
                        IsActive = true,
                        DisplayOrder = 4
                    }
                );
                await context.SaveChangesAsync();
            }
            else
            {
                // Self-healing: Ensure all 4 delivery options exist in the database
                var existingCodes = context.DeliveryMethods.Select(d => d.Code).ToList();
                var methodsToAdd = new List<DeliveryMethod>();

                if (!existingCodes.Contains("VETExpress"))
                {
                    methodsToAdd.Add(new DeliveryMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "VET Express",
                        KhmerName = "វីរៈ ប៊ុនថាំ អេចប្រេស",
                        Code = "VETExpress",
                        BaseFee = 2.00m,
                        EstimatedDeliveryTime = "1-2 Business days (24 Provinces)",
                        RequiresBranchSelection = true,
                        IsActive = true,
                        DisplayOrder = 2
                    });
                }

                if (!existingCodes.Contains("CityDelivery"))
                {
                    methodsToAdd.Add(new DeliveryMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "Phnom Penh City Delivery",
                        KhmerName = "ដឹកជញ្ជូនក្នុង រាជធានីភ្នំពេញ",
                        Code = "CityDelivery",
                        BaseFee = 1.50m,
                        EstimatedDeliveryTime = "Same Day (2-4 Hours)",
                        RequiresBranchSelection = false,
                        IsActive = true,
                        DisplayOrder = 3
                    });
                }

                if (!existingCodes.Contains("OtherExpress"))
                {
                    methodsToAdd.Add(new DeliveryMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "Other Express Carriers",
                        KhmerName = "ក្រុមហ៊ុនដឹកជញ្ជូនផ្សេងទៀត",
                        Code = "OtherExpress",
                        BaseFee = 2.00m,
                        EstimatedDeliveryTime = "1-3 Business days",
                        RequiresBranchSelection = false,
                        IsActive = true,
                        DisplayOrder = 4
                    });
                }

                if (methodsToAdd.Any())
                {
                    context.DeliveryMethods.AddRange(methodsToAdd);
                    await context.SaveChangesAsync();
                }
            }

            if (!context.DeliveryBranches.Any())
            {
                context.DeliveryBranches.AddRange(
                    // Phnom Penh
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "រាជធានីភ្នំពេញ", BranchName = "សាខាក្បាលថ្នល់ (ផ្លូវ ២៧១) (ផ្សារដើមថ្កូវ)", Address = "Street 271, Khan Chamkarmon, Phnom Penh", Phone = "012345678" },
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "រាជធានីភ្នំពេញ", BranchName = "សាខាមេគង្គ (ផ្សារចាស់)", Address = "Street 108, Khan Daun Penh, Phnom Penh", Phone = "012345679" },
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "រាជធានីភ្នំពេញ", BranchName = "សាខាអូរឬស្សី", Address = "Street 182, Khan 7 Makara, Phnom Penh", Phone = "012345680" },

                    // Siem Reap
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "ខេត្តសៀមរាប", BranchName = "សាខាសៀមរាប ក្រុង (ផ្លូវ ៦០)", Address = "Road 60, Siem Reap City", Phone = "063123456" },
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "ខេត្តសៀមរាប", BranchName = "សាខាផ្សារលើ សៀមរាប", Address = "National Road 6, Siem Reap", Phone = "063123457" },

                    // Battambang
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "ខេត្តបាត់ដំបង", BranchName = "សាខាបាត់ដំបង ក្រុង (ផ្លូវ ជាតិលេខ ៥)", Address = "National Road 5, Battambang City", Phone = "053123456" },

                    // Preah Sihanouk
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "ខេត្តព្រះសីហនុ", BranchName = "សាខាកំពង់សោម ក្រុង", Address = "Ekareach Street, Sihanoukville", Phone = "034123456" },

                    // Kampong Cham
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "ខេត្តកំពង់ចាម", BranchName = "សាខាកំពង់ចាម ក្រុង", Address = "Moniwong Boulevard, Kampong Cham", Phone = "042123456" },

                    // Kampot
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "ខេត្តកំពត", BranchName = "សាខាកំពត ក្រុង", Address = "Riverside Road, Kampot City", Phone = "033123456" },

                    // Kandal
                    new DeliveryBranch { CarrierCode = "VETExpress", Province = "ខេត្តកណ្តាល", BranchName = "សាខាតាខ្មៅ (ក្រុងតាខ្មៅ)", Address = "Takhmao City Center, Kandal", Phone = "024123456" }
                );
                await context.SaveChangesAsync();
            }

            // ==========================================
            // 16. HOMEPAGE SETTINGS
            // ==========================================
            try
            {
                if (!context.HomePageSettings.Any())
                {
                    context.HomePageSettings.Add(new HomePageSettings
                    {
                        PromoTag = "Flash Sale",
                        PromoText = "Season Collection — Up to 40% OFF essentials & trending styles!",
                        PromoCtaText = "Shop Now",
                        PromoCtaUrl = "/Shop?onSale=true",
                        HeroLabel = "NEW COLLECTION 2026",
                        HeroTitle = "Wear your identity.",
                        HeroHighlightWord = "identity.",
                        HeroDescription = "Discover modern clothing designed for everyday confidence, comfort and effortless style.",
                        HeroPrimaryBtnText = "Shop Collection",
                        HeroPrimaryBtnUrl = "/Shop",
                        HeroSecondaryBtnText = "Explore Products",
                        HeroSecondaryBtnUrl = "/Shop",
                        HeroImageUrl = "https://images.unsplash.com/photo-1445205170230-053b83016050?auto=format&fit=crop&w=1000&q=85",
                        FloatingCard1Title = "New Season",
                        FloatingCard1Sub = "Fresh styles are here",
                        FloatingCard2Title = "Free Shipping",
                        FloatingCard2Sub = "On orders over $50",
                        Stat1Value = "500+",
                        Stat1Label = "PRODUCTS",
                        Stat2Value = "50+",
                        Stat2Label = "BRANDS",
                        Stat3Value = "10K+",
                        Stat3Label = "CUSTOMERS"
                    });
                    await context.SaveChangesAsync();
                }

                if (!context.StoreReceiptSettings.Any())
                {
                    context.StoreReceiptSettings.Add(new StoreReceiptSettings
                    {
                        StoreName = "CLOTHÉ",
                        StoreNameKh = "ហាងសម្លៀកបំពាក់ CLOTHÉ",
                        Tagline = "Atelier & Luxury Fashion House",
                        TaglineKh = "ម៉ូដទាន់សម័យ និងប្រណីតភាព",
                        Address = "#88 Preah Norodom Blvd, BKK1, Phnom Penh, Cambodia",
                        AddressKh = "អគារលេខ ៨៨ មហាវិថីព្រះនរោត្តម សង្កាត់បឹងកេងកង១ រាជធានីភ្នំពេញ",
                        Phone = "+855 (0) 23 999 888",
                        Email = "info@clothe-atelier.com",
                        Website = "https://clothe-store.com",
                        Telegram = "@clothe_support",
                        VatNumber = "K005-902201889",
                        ReceiptPrefix = "REC-",
                        DefaultReceiptTheme = "ModernLuxury",
                        ReturnPolicyEn = "Items may be exchanged within 7 days of purchase with original receipt and tags attached. No cash refunds.",
                        ReturnPolicyKh = "ទំនិញដែលបានទិញរួចអាចប្តូរបានក្នុងរយៈពេល ៧ ថ្ងៃ ដោយមានវិក្កយបត្រ និងស្លាកសញ្ញាដើម។ មិនមានការបង្វិលប្រាក់វិញទេ។",
                        ThankYouNoteEn = "Thank you for shopping at CLOTHÉ! We appreciate your patronage.",
                        ThankYouNoteKh = "សូមអរគុណសម្រាប់ការគាំទ្រហាងយើងខ្ញុំ! សូមអញ្ជើញមកម្តងទៀត។",
                        ShowDualCurrency = true,
                        ShowKhqr = true,
                        ExchangeRate = 4100m,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = "System Initializer"
                    });
                    await context.SaveChangesAsync();
                }
            }
            catch { }
        }
    }
}