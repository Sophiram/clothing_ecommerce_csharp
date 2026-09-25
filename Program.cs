using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// LOAD .ENV ENVIRONMENT VARIABLES
// ========================================
var possibleEnvPaths = new[]
{
    Path.Combine(builder.Environment.ContentRootPath, ".env"),
    Path.Combine(builder.Environment.ContentRootPath, "..", ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env")
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
                builder.Configuration[key] = val;
            }
        }
        break;
    }
}

// Add HttpClient for Bakong Open API
builder.Services.AddHttpClient("BakongApi", client =>
{
    var baseUrl = builder.Configuration["BAKONG_BASE_URL"] ?? builder.Configuration["KHQR_BASE_URL"] ?? "https://api-bakong.nbc.gov.kh";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(3);
});

// ========================================
// DATABASE
// ========================================

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});


// ========================================
// IDENTITY
// ========================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

// ========================================
// EXTERNAL AUTHENTICATION (GOOGLE)
// ========================================
var googleClientId = builder.Configuration["GOOGLE_CLIENT_ID"];
var googleClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SignInScheme = IdentityConstants.ExternalScheme;
        });
}

// ========================================
// REPOSITORIES & UNIT OF WORK
// ========================================
builder.Services.AddScoped(typeof(WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IRepository<>), typeof(WebApplication_ClothingEcommerce.Data.Repositories.Implementations.Repository<>));
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.ICartRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.CartRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IProductRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.ProductRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.ICategoryRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.CategoryRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IBrandRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.BrandRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IOrderRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.OrderRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.ICustomerRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.CustomerRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IReviewRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.ReviewRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IWishlistRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.WishlistRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IInventoryRepository, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.InventoryRepository>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Data.Repositories.Interfaces.IUnitOfWork, WebApplication_ClothingEcommerce.Data.Repositories.Implementations.UnitOfWork>();

// ========================================
// SERVICES
// ========================================
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IColorService, ColorService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IKhqrService, KhqrService>();
builder.Services.AddScoped<IBakongPaymentService, BakongPaymentService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductVariantService, ProductVariantService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService, AvatarService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IVetExpressService, VetExpressService>();
builder.Services.AddHttpClient<ITelegramService, TelegramService>();
builder.Services.AddScoped<ISizeService, SizeService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.Interfaces.IEmailService, EmailService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.Interfaces.IReportService, ReportService>();
builder.Services.AddScoped<WebApplication_ClothingEcommerce.Services.Interfaces.IStoreSettingsService, WebApplication_ClothingEcommerce.Services.StoreSettingsService>();

// ========================================
// MVC
// ========================================

builder.Services.AddControllersWithViews();


// ========================================
// SESSION
// ========================================

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

var app = builder.Build();


// ========================================
// MIDDLEWARE
// ========================================

app.UseForwardedHeaders();

// Production Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://code.jquery.com https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
        "font-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.gstatic.com data:; " +
        "img-src 'self' data: https: blob:; " +
        "connect-src 'self' https:;");

    await next();
});

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


// ========================================
// ROUTING
// ========================================

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



await AppDbInitializer.Seed(app);

app.Run();