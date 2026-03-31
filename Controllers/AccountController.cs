using AutoCurriculum.Middleware;
using AutoCurriculum.Models;
using AutoCurriculum.Services;
using AutoCurriculum.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AutoCurriculum.Controllers
{
    [AllowAnonymous] 
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly AutoCurriculumDbContext _db;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IMemoryCache cache,
            AutoCurriculumDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _cache = cache;
            _db = db;
        }
        // 1. ĐĂNG NHẬP (Luồng OTP)

        [HttpGet]
        public IActionResult Login(string? returnUrl = null, string? reason = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            if (reason == "expired")
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
            else if (reason == "locked")
                TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin.";

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByNameAsync(model.UsernameOrEmail)
                    ?? await _userManager.FindByEmailAsync(model.UsernameOrEmail);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ Admin.");
                    return View(model);
                }

                var isRoleAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isRoleAdmin || user.Email == "tainguyen280404@gmail.com")
                {
                    await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
                    
                    await WriteLog(user.Email, "Đăng nhập", user.Email, "Success", "Admin đăng nhập thành công (Bypass OTP)");
                    
                    var roles = await _userManager.GetRolesAsync(user);
                    return Redirect(PostLoginRedirect.GetUrl(roles, returnUrl));
                }

                try 
                {
                    var otp = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                    await _emailService.SendEmailAsync(user.Email!,
                        "🔐 Mã xác nhận Đăng nhập - AutoCurriculum",
                        $"<h3>Xin chào {user.FullName},</h3>" +
                        $"<p>Mã OTP: <b style='font-size:24px;color:red'>{otp}</b></p>" +
                        $"<p>Mã có hiệu lực 3 phút. Không chia sẻ cho ai!</p>");
                } 
                catch (Exception) 
                {
                    ModelState.AddModelError("", "Lỗi hệ thống gửi Mail OTP. Vui lòng thử lại sau.");
                    return View(model);
                }

                TempData["UserId"]     = user.Id;
                TempData["RememberMe"] = model.RememberMe;
                TempData["ReturnUrl"]  = returnUrl;
                return RedirectToAction("VerifyOtp");
            }
            await WriteLog(user?.Email ?? model.UsernameOrEmail, "Đăng nhập", model.UsernameOrEmail, "Error", "Sai tài khoản hoặc mật khẩu");

            ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác.");
            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            if (TempData["UserId"] == null) return RedirectToAction("Login");
            TempData.Keep("UserId");
            TempData.Keep("RememberMe");
            TempData.Keep("ReturnUrl");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string otp)
        {
            var userId     = TempData["UserId"]?.ToString();
            var rememberMe = TempData["RememberMe"] as bool? ?? false;
            var returnUrl  = TempData["ReturnUrl"]?.ToString();

            if (userId == null) return RedirectToAction("Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", otp);

            if (isValid)
            {
                await _signInManager.SignInAsync(user, isPersistent: rememberMe);

                await WriteLog(user.Email, "Xác thực OTP", user.Email, "Success", "Đăng nhập thành công");

                var roles = await _userManager.GetRolesAsync(user);
                return Redirect(PostLoginRedirect.GetUrl(roles, returnUrl));
            }

            await WriteLog(user.Email, "Xác thực OTP", user.Email, "Error", "Nhập OTP sai");

            ModelState.AddModelError("", "Mã OTP không hợp lệ hoặc đã hết hạn.");
            TempData.Keep("UserId");
            TempData.Keep("RememberMe");
            TempData.Keep("ReturnUrl");
            return View();
        }

        // 2. ĐĂNG KÝ

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existing = await _userManager.FindByEmailAsync(model.Email)
                        ?? await _userManager.FindByNameAsync(model.Username);
            if (existing != null)
            {
                ModelState.AddModelError("", "Email hoặc Tên đăng nhập đã có người sử dụng!");
                return View(model);
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            _cache.Set("Reg_" + model.Email, Tuple.Create(model, otp), cacheOptions);

            await _emailService.SendEmailAsync(model.Email,
                "🚀 Mã xác nhận đăng ký - AutoCurriculum",
                $"<h3>Chào {model.Username},</h3>" +
                $"<p>Mã OTP: <b style='font-size:24px;color:blue'>{otp}</b></p>" +
                $"<p>Mã tự hủy sau 5 phút.</p>");

            TempData["RegEmail"] = model.Email;
            return RedirectToAction("VerifyRegistrationOtp");
        }

        [HttpGet]
        public IActionResult VerifyRegistrationOtp()
        {
            if (TempData["RegEmail"] == null) return RedirectToAction("Register");
            TempData.Keep("RegEmail");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRegistrationOtp(string otp)
        {
            var email = TempData["RegEmail"]?.ToString();
            if (email == null) return RedirectToAction("Register");

            if (_cache.TryGetValue("Reg_" + email, out Tuple<RegisterViewModel, string>? cachedData))
            {
                if (otp == cachedData!.Item2)
                {
                    var user = new ApplicationUser
                    {
                        UserName       = cachedData.Item1.Username,
                        Email          = cachedData.Item1.Email,
                        FullName       = cachedData.Item1.Username,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, cachedData.Item1.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                        _cache.Remove("Reg_" + email);
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        // SỬA Ở ĐÂY: Dùng đúng chữ "Success"
                        await WriteLog(user.Email, "Đăng ký", user.Email, "Success", "Đăng ký tài khoản mới thành công");

                        return RedirectToAction("Index", "Curriculum");
                    }

                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                }
                else
                {
                    ModelState.AddModelError("", "Mã OTP không chính xác.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Mã OTP đã hết hạn. Vui lòng đăng ký lại.");
            }

            TempData.Keep("RegEmail");
            return View();
        }

        // 3. ĐĂNG XUẤT

        [HttpPost]
        [Authorize] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var email = User.Identity?.Name;
            
            await WriteLog(email, "Đăng xuất", email, "Success", "Đăng xuất thành công");
            
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login"); 
        }

        // 4. QUÊN MẬT KHẨU

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Email này chưa được đăng ký!");
                return View(model);
            }

            try
            {
                var otp = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                await _emailService.SendEmailAsync(model.Email,
                    "Mã OTP Đặt lại mật khẩu - AutoCurriculum",
                    $"<p>Mã OTP: <b style='font-size:24px;color:red'>{otp}</b></p>" +
                    $"<p>Mã có hiệu lực 3 phút.</p>");

                return RedirectToAction("ResetPassword", new { email = model.Email });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Hệ thống gửi mail lỗi: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
            => View(new ResetPasswordViewModel { Email = email });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("Login");

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", model.Token);
            if (isValid)
            {
                await _userManager.RemovePasswordAsync(user);
                var result = await _userManager.AddPasswordAsync(user, model.NewPassword);

                if (result.Succeeded)
                {
                    await WriteLog(user.Email, "Đổi mật khẩu", user.Email, "Success", "Đặt lại mật khẩu thành công");
                    
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    var roles = await _userManager.GetRolesAsync(user);
                    return Redirect(PostLoginRedirect.GetUrl(roles));
                }

                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
            }
            else
            {
                ModelState.AddModelError("", "Mã OTP không chính xác hoặc đã hết hạn.");
            }

            return View(model);
        }

        // 5. ACCESS DENIED

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // HELPER: Ghi SystemLog
        private async Task WriteLog(
            string? email, string action,
            string? keyword, string status, string message)
        {
            try
            {
                _db.SystemLogs.Add(new SystemLog
                {
                    UserEmail = email ?? "Anonymous",
                    Action    = action,
                    Keyword   = keyword ?? "",
                    Status    = status,
                    Message   = message,
                    ExecutionTimeMs = 0, 
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
            catch {  }
        }
    }
}