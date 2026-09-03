# WebApplication_ClothingEcommerce

Complete ASP.NET Core MVC clothing e-commerce starter with:

- Public home page and shop
- Product details
- Cart and wishlist
- Checkout and orders
- Identity login/register/logout
- Customer profile
- Admin dashboard
- Product/category/brand management
- Customer management
- Inventory management
- SQL Server + EF Core
- Seed data and admin account

## Requirements
- .NET 9 SDK
- SQL Server / LocalDB
- Visual Studio 2022 or `dotnet` CLI

## Run

```bash
dotnet restore
dotnet ef database update
dotnet run
```

If EF CLI is not installed:

```bash
dotnet tool install --global dotnet-ef
```

The application also calls `Database.MigrateAsync()` during startup.

## Default admin
Email: `admin@clothing.com`
Password: `Admin@123`

Change this password before using the application anywhere public.

## Important
The seed uses remote image URLs. For production, use your own `/wwwroot/uploads/products` image storage or cloud object storage.
