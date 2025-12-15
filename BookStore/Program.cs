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

// ✅ Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BookStore API",
        Version = "v1",
        Description = "API documentation for BookStore - Test và kiểm tra các API endpoints\n\n" +
                      "📌 Lưu ý:\n" +
                      "- Các API có [Authorize] cần đăng nhập trước\n" +
                      "- API Admin cần quyền Admin\n" +
                      "- Một số API đã bỏ ValidateAntiForgeryToken để test qua Swagger"
    });
    
    // Cấu hình để Swagger phát hiện cả MVC Controllers
    c.CustomSchemaIds(type => type.FullName);
    
    // Cho phép Swagger phát hiện các action có Route attribute
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        // Chỉ hiển thị các action có Route attribute hoặc trả về JSON
        return apiDesc.RelativePath != null;
    });
    
    // Thêm security definition cho cookie authentication
    c.AddSecurityDefinition("cookieAuth", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Cookie,
        Name = ".AspNetCore.Identity.Application",
        Description = "Cookie authentication (đăng nhập trước khi test API có [Authorize])"
    });
    
    // Include XML comments nếu có
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.Cookie.Name = ".BookStore.Session";
    opt.IdleTimeout = TimeSpan.FromMinutes(60);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, SessionCartService>();
builder.Services.AddScoped<IQKTPaymentService, QKTPaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSender>();
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
    
    // ✅ Swagger UI (chỉ trong Development)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookStore API v1");
        c.RoutePrefix = "swagger"; // Truy cập tại /swagger
        c.DocumentTitle = "BookStore API Documentation";
    });
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

    // 🟢 SỬA 1: Thêm *.tawk.to (cho phép ảnh/icon của Tawk.to)
    "img-src 'self' data: blob: https: *.tawk.to",

    // 🟢 SỬA 2: Thêm *.tawk.to (cho phép font của Tawk.to)
    "font-src 'self' data: fonts.gstatic.com cdnjs.cloudflare.com cdn.jsdelivr.net *.tiny.cloud *.tawk.to",

    // 🟢 SỬA 3: Thêm *.tawk.to (cho phép CSS của Tawk.to)
    "style-src 'self' 'unsafe-inline' fonts.googleapis.com cdnjs.cloudflare.com cdn.jsdelivr.net *.tiny.cloud *.tawk.to",

    // 🟢 SỬA 4: Thêm *.tawk.to (cho phép SCRIPT của Tawk.to)
    "script-src 'self' 'unsafe-inline' 'unsafe-eval' cdn.jsdelivr.net cdnjs.cloudflare.com *.tiny.cloud *.tawk.to",

    // 🟢 SỬA 5: Thêm *.tawk.to (cho phép Tawk.to kết nối server)
    "connect-src 'self' cdn.jsdelivr.net *.tiny.cloud *.tawk.to wss://*.tawk.to",

    // 🟢 SỬA 6: Thêm *.tawk.to (cho phép Tawk.to chạy trong frame)
    // 🗺️ Thêm Google Maps (cho phép nhúng bản đồ)
    "frame-src 'self' *.tawk.to *.google.com maps.google.com",

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
