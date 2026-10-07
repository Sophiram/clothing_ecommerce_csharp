using ClothingEcommerce.Client.Services.ApiClient;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// LOAD .ENV FILE
// ========================================
var possibleEnvPaths = new[]
{
    Path.Combine(builder.Environment.ContentRootPath, ".env"),
    Path.Combine(builder.Environment.ContentRootPath, "..", ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), ".env")
};

foreach (var envPath in possibleEnvPaths)
{
    if (File.Exists(envPath))
    {
        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var val = parts[1].Trim().Trim('"').Trim('\'');
                Environment.SetEnvironmentVariable(key, val);
            }
        }
        break;
    }
}
builder.Configuration.AddEnvironmentVariables();

// ========================================
// MVC & HTTP ACCESSOR
// ========================================
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ========================================
// AUTHENTICATION (COOKIE + EXTERNAL GOOGLE)
// ========================================
var googleClientId = builder.Configuration["GOOGLE_CLIENT_ID"];
var googleClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"];

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "Clothe.Client.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
})
.AddCookie("ExternalCookie", options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
});

if (!string.IsNullOrWhiteSpace(googleClientId) && 
    !string.IsNullOrWhiteSpace(googleClientSecret) && 
    !googleClientId.Contains("your-google", StringComparison.OrdinalIgnoreCase))
{
    authBuilder.AddGoogle("Google", options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SaveTokens = true;
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("AdminAccess", policy => policy.RequireRole("Admin", "SuperAdmin"));
});

// ========================================
// DATABASE & EF CORE
// ========================================
var connectionString = builder.Configuration["DefaultConnection"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=ClothingEcommerceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<WebApplication_ClothingEcommerce.Data.AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// ========================================
// IDENTITY CORE (FOR ADMIN USER / ROLE MANAGEMENT)
// ========================================
builder.Services.AddIdentityCore<WebApplication_ClothingEcommerce.Models.ApplicationUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
.AddEntityFrameworkStores<WebApplication_ClothingEcommerce.Data.AppDbContext>();

// ========================================
// REPOSITORIES
// ========================================
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IUnitOfWork, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.UnitOfWork>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IProductRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.ProductRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.ICategoryRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.CategoryRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IBrandRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.BrandRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.ICartRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.CartRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IOrderRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.OrderRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.ICustomerRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.CustomerRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IReviewRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.ReviewRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IWishlistRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.WishlistRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IInventoryRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.InventoryRepository>();

// ========================================
// DOMAIN SERVICES FOR ADMIN CONTROLLERS
// ========================================
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IProductService, WebApplication_ClothingEcommerce.Services.ProductService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.ICategoryService, WebApplication_ClothingEcommerce.Services.CategoryService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IBrandService, WebApplication_ClothingEcommerce.Services.BrandService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.ISizeService, WebApplication_ClothingEcommerce.Services.SizeService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IColorService, WebApplication_ClothingEcommerce.Services.ColorService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IProductVariantService, WebApplication_ClothingEcommerce.Services.ProductVariantService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IProductImageService, WebApplication_ClothingEcommerce.Services.ProductImageService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IOrderService, WebApplication_ClothingEcommerce.Services.OrderService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IPaymentService, WebApplication_ClothingEcommerce.Services.PaymentService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IShipmentService, WebApplication_ClothingEcommerce.Services.ShipmentService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IDeliveryService, WebApplication_ClothingEcommerce.Services.DeliveryService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IVetExpressService, WebApplication_ClothingEcommerce.Services.VetExpressService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IInventoryService, WebApplication_ClothingEcommerce.Services.InventoryService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.Interfaces.IReportService, WebApplication_ClothingEcommerce.Services.ReportService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IAdminUserService, WebApplication_ClothingEcommerce.Services.AdminUserService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IAuditService, WebApplication_ClothingEcommerce.Services.AuditService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService, WebApplication_ClothingEcommerce.Services.AvatarService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.ICustomerService, WebApplication_ClothingEcommerce.Services.CustomerService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IReviewService, WebApplication_ClothingEcommerce.Services.ReviewService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IWishlistService, WebApplication_ClothingEcommerce.Services.WishlistService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.ITelegramService, WebApplication_ClothingEcommerce.Services.TelegramService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.Interfaces.IEmailService, WebApplication_ClothingEcommerce.Services.EmailService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IKhqrService, WebApplication_ClothingEcommerce.Services.KhqrService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IBakongPaymentService, WebApplication_ClothingEcommerce.Services.BakongPaymentService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.ICartService, WebApplication_ClothingEcommerce.Services.CartService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IHomeService, WebApplication_ClothingEcommerce.Services.HomeService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IShopService, WebApplication_ClothingEcommerce.Services.ShopService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.IProfileService, WebApplication_ClothingEcommerce.Services.ProfileService>();

// ========================================
// TYPED HTTP CLIENT (API CLIENT)
// ========================================
var apiBaseUrl = builder.Configuration["API_BASE_URL"]
    ?? builder.Configuration["ApiSettings:BaseUrl"]
    ?? "http://localhost:5001";

builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.Interfaces.IStoreSettingsService, ClothingEcommerce.Client.Services.ClientStoreSettingsService>();

builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<AuthHeaderHandler>();

var app = builder.Build();

// ========================================
// HTTP PIPELINE
// ========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Area routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
