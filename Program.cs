using AutoCurriculum.Models;
using AutoCurriculum.Repositories.Interfaces;
using AutoCurriculum.Repositories.Implementations;
using AutoCurriculum.Services.Interfaces;
using AutoCurriculum.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AutoCurriculum.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────
builder.Services.AddDbContext<AutoCurriculumDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── HttpClient (đặt header User-Agent cho Wikipedia) ─────────
builder.Services.AddHttpClient("Wikipedia", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "AutoCurriculumApp/1.0 (contact@example.com)");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── Repositories ─────────────────────────────────────────────
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<IContentRepository, ContentRepository>();

// ── External Services ─────────────────────────────────────────
builder.Services.AddScoped<IWikipediaService, WikipediaService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();

// ── Business Services ─────────────────────────────────────────
builder.Services.AddScoped<ICurriculumService, CurriculumService>();

// ── Bưu tá gửi mail OTP ───────────────────────────────────────
builder.Services.AddScoped<IEmailService, EmailService>();

// ── MVC ───────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// 1. ĐĂNG KÝ IDENTITY CHÍNH THỨC
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Cấu hình mật khẩu đơn giản lúc làm đồ án
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AutoCurriculumDbContext>()
.AddDefaultTokenProviders(); // <--- CHÌA KHÓA TẠO OTP 6 SỐ NẰM Ở ĐÂY

// 2. Cấu hình Cookie (Khi bị [Authorize] chặn, tự động văng ra trang Login)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ĐĂNG KÝ ROTATIVA (Bắt buộc phải nằm dưới UseStaticFiles)
Rotativa.AspNetCore.RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

app.UseRouting();

// BẮT BUỘC PHẢI CÓ DÒNG NÀY TRƯỚC AUTHORIZATION ĐỂ WEB NHẬN DIỆN USER ĐÃ ĐĂNG NHẬP
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();