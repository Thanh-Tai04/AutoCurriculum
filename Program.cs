using AutoCurriculum.Models;
using AutoCurriculum.Repositories.Interfaces;
using AutoCurriculum.Repositories.Implementations;
using AutoCurriculum.Services.Interfaces;
using AutoCurriculum.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AutoCurriculum.Services;
using AutoCurriculum.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<AutoCurriculumDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── HttpClient ────────────────────────────────────────────────
builder.Services.AddHttpClient("Wikipedia", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "AutoCurriculumApp/1.0 (contact@example.com)");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── Repositories ──────────────────────────────────────────────
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<IContentRepository, ContentRepository>();

// ── Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IWikipediaService, WikipediaService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<ICurriculumService, CurriculumService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ── Identity ──────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AutoCurriculumDbContext>()
.AddDefaultTokenProviders();

// ── Cookie ────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // ← Thêm: session 8 tiếng
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// ═════════════════════════════════════════════════════════════
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Gọi file SeedData vừa tạo ở trên
        await AutoCurriculum.Data.SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi khi tạo tài khoản Admin mặc định.");
    }
}

// ── Seed Roles & Admin ────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Tạo 2 roles
    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Tạo tài khoản Admin mặc định
    var adminEmail = "admin@autocurriculum.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Administrator",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, "Admin@123456");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}

// ── Middleware Pipeline (THỨ TỰ RẤT QUAN TRỌNG) ──────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
Rotativa.AspNetCore.RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

app.UseRouting();        // ← Phải trước Authentication
app.UseAuthentication(); // ← Phải trước Authorization
app.UseAuthorization();
app.UseRoleAccessMiddleware();

// ── Routes ────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "MyArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
