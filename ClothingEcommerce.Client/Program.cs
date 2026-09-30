using ClothingEcommerce.Client.Services.ApiClient;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

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
// AUTHENTICATION (COOKIE WITH JWT PERSISTENCE)
// ========================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "Clothe.Client.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("AdminAccess", policy => policy.RequireRole("Admin", "SuperAdmin"));
});

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
