using AutoCurriculum.Models;
using Microsoft.AspNetCore.Identity;

namespace AutoCurriculum.Middleware
{
    public class RoleAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RoleAccessMiddleware> _logger;

        // Các path không cần kiểm tra (tránh vòng lặp redirect)
        private static readonly string[] PublicPaths =
        [
            "/account/login",
            "/account/register",
            "/account/accessdenied",
            "/account/forgotpassword",
            "/home",
            "/favicon.ico",
            "/lib",
            "/css",
            "/js"
        ];

        public RoleAccessMiddleware(RequestDelegate next, ILogger<RoleAccessMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            AutoCurriculumDbContext db)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var user = context.User;

            // ══════════════════════════════════════════════════════
            // 0. CHẶN ADMIN ĐI LẠC VÀO GIAO DIỆN CỦA USER
            // ══════════════════════════════════════════════════════
            if (user.Identity?.IsAuthenticated == true && user.IsInRole("Admin"))
            {
                // Những đường dẫn hợp lệ mà Admin được phép truy cập
                bool isAllowedForAdmin = path.StartsWith("/admin") ||
                                         path.StartsWith("/account/logout") ||
                                         path.StartsWith("/account/accessdenied") ||
                                         path.StartsWith("/lib") ||
                                         path.StartsWith("/css") ||
                                         path.StartsWith("/js") ||
                                         path.StartsWith("/images") ||
                                         path.EndsWith(".ico");

                if (!isAllowedForAdmin)
                {
                    // Trả Admin về đúng "Lãnh địa" của mình
                    context.Response.Redirect("/Admin/Dashboard");
                    return;
                }
            }

            // ══════════════════════════════════════════════════════
            // Bỏ qua các path public (cho khách hoặc User thường)
            // ══════════════════════════════════════════════════════
            if (PublicPaths.Any(p => path.StartsWith(p)))
            {
                await _next(context);
                return;
            }

            // ══════════════════════════════════════════════════════
            // 1. CHẶN TRUY CẬP TRANG ADMIN (Dành cho User thường)
            // ══════════════════════════════════════════════════════
            if (path.StartsWith("/admin"))
            {
                if (user.Identity?.IsAuthenticated != true)
                {
                    var returnUrl = Uri.EscapeDataString(context.Request.Path);
                    context.Response.Redirect($"/Account/Login?returnUrl={returnUrl}");
                    return;
                }

                if (!user.IsInRole("Admin"))
                {
                    // Log truy cập trái phép vào DB
                    await WriteLog(db, new SystemLog
                    {
                        UserEmail = user.Identity?.Name ?? "Unknown",
                        Action   = "UNAUTHORIZED_ACCESS",
                        Keyword  = context.Request.Path,
                        Status   = "BLOCKED",
                        Message  = $"User cố truy cập trang Admin | IP: {context.Connection.RemoteIpAddress}",
                        CreatedAt = DateTime.Now
                    });

                    _logger.LogWarning("⛔ Truy cập trái phép: {Email} → {Path}",
                        user.Identity?.Name, context.Request.Path);

                    context.Response.Redirect("/Account/AccessDenied");
                    return;
                }
            }

            // ══════════════════════════════════════════════════════
            // 2. KIỂM TRA SESSION HẾT HẠN (User bị xóa khỏi DB)
            // ══════════════════════════════════════════════════════
            if (user.Identity?.IsAuthenticated == true)
            {
                var appUser = await userManager.GetUserAsync(user);

                if (appUser == null)
                {
                    _logger.LogWarning("⚠️ Session không hợp lệ, user không còn trong DB: {Path}", path);
                    context.Response.Redirect("/Account/Login?reason=expired");
                    return;
                }

                // Kiểm tra tài khoản bị khóa
                if (appUser.LockoutEnd != null && appUser.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    await WriteLog(db, new SystemLog
                    {
                        UserEmail = appUser.Email,
                        Action   = "LOCKED_ACCOUNT_ACCESS",
                        Keyword  = context.Request.Path,
                        Status   = "BLOCKED",
                        Message  = "Tài khoản bị khóa cố truy cập hệ thống",
                        CreatedAt = DateTime.Now
                    });

                    context.Response.Redirect("/Account/Login?reason=locked");
                    return;
                }
            }

            await _next(context);
        }

        // ══════════════════════════════════════════════════════════
        // 3. HELPER: Ghi log an toàn, không crash app nếu DB lỗi
        // ══════════════════════════════════════════════════════════
        private static async Task WriteLog(AutoCurriculumDbContext db, SystemLog log)
        {
            try
            {
                db.SystemLogs.Add(log);
                await db.SaveChangesAsync();
            }
            catch
            {
                // Swallow exception — log không được làm crash app
            }
        }
    }

    // Extension method để đăng ký gọn trong Program.cs
    public static class RoleAccessMiddlewareExtensions
    {
        public static IApplicationBuilder UseRoleAccessMiddleware(this IApplicationBuilder app)
            => app.UseMiddleware<RoleAccessMiddleware>();
    }
}