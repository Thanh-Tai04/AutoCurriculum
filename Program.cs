using AutoCurriculum.Models;
using AutoCurriculum.Repositories.Interfaces;
using AutoCurriculum.Repositories.Implementations;
using AutoCurriculum.Services.Interfaces;
using AutoCurriculum.Services.Implementations;
using Microsoft.EntityFrameworkCore;

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

// Add services to the container.
builder.Services.AddControllersWithViews();

// ── MVC ───────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Curriculum}/{action=Index}/{id?}");

app.Run();