using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BookStore.Data;
using BookStore.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(opt => opt.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.Cookie.Name = ".BookStore.Session";
    opt.IdleTimeout = TimeSpan.FromMinutes(60);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, SessionCartService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
// ✅ Bật nén Gzip/Brotli
builder.Services.AddResponseCompression(opt =>
{
    opt.EnableForHttps = true;
});

// Culture mặc định VN
var culture = new System.Globalization.CultureInfo("vi-VN");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

var app = builder.Build();

// Ensure DB up to date + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    // await Seed.EnsureSeedAsync(scope.ServiceProvider);
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 🔒 Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["X-XSS-Protection"] = "0";
    ctx.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    ctx.Response.Headers["Cross-Origin-Resource-Policy"] = "same-site";

    var csp = string.Join("; ",
    "default-src 'self'",

    // 🟢 SỬA: Thêm 'blob:' (cho phép ảnh upload)
    "img-src 'self' data: blob: https:",

    "font-src 'self' data: fonts.gstatic.com cdnjs.cloudflare.com cdn.jsdelivr.net *.tiny.cloud",

    // 🟢 SỬA: Thêm *.tiny.cloud
    "style-src 'self' 'unsafe-inline' fonts.googleapis.com cdnjs.cloudflare.com cdn.jsdelivr.net *.tiny.cloud",

    // 🟢 SỬA: Thêm 'unsafe-eval' và *.tiny.cloud
    "script-src 'self' 'unsafe-inline' 'unsafe-eval' cdn.jsdelivr.net cdnjs.cloudflare.com *.tiny.cloud",

    // 🟢 SỬA: Thêm *.tiny.cloud
    "connect-src 'self' cdn.jsdelivr.net *.tiny.cloud",

    "frame-ancestors 'self'"
);

    ctx.Response.Headers["Content-Security-Policy"] = csp;

    await next();
});

// ✅ Response Compression
app.UseResponseCompression();

// 📦 Static file cache (7 ngày)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = c =>
    {
        c.Context.Response.Headers["Cache-Control"] =
            "public,max-age=604800,immutable";
    }
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
