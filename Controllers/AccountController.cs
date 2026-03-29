using AutoCurriculum.Models;
using AutoCurriculum.Services;
using AutoCurriculum.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AutoCurriculum.Controllers
{
    [Authorize] // Khóa toàn bộ các hành động trong Controller này
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService; 
        private readonly IMemoryCache _cache;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailService emailService, IMemoryCache cache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _cache = cache;
        }

        // ==========================================
        // 1. ĐĂNG NHẬP (Luồng OTP)
        // ==========================================

        // Hiển thị form Đăng nhập
        [HttpGet]
        [AllowAnonymous] // Mở cửa cho người chưa đăng nhập vào xem form
        public IActionResult Login(string returnUrl = "/")
        {
            if (User.Identity != null && User.Identity.IsAuthenticated) return RedirectToAction("Index", "Curriculum");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Xử lý khi bấm nút Đăng nhập
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            // Tìm user bằng Username hoặc Email
            var user = await _userManager.FindByNameAsync(model.UsernameOrEmail) ?? 
                       await _userManager.FindByEmailAsync(model.UsernameOrEmail);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // Mật khẩu đúng -> Tạo mã OTP 6 số
                var otp = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

                // Gửi email
                string subject = "🔐 Mã xác nhận Đăng nhập - AutoCurriculum";
                string message = $"<h3>Xin chào {user.FullName},</h3>" +
                                 $"<p>Mã OTP đăng nhập của bạn là: <b style='font-size: 24px; color: red;'>{otp}</b></p>" +
                                 $"<p>Mã này có hiệu lực trong vòng 3 phút. Vui lòng không chia sẻ cho bất kỳ ai!</p>";
                await _emailService.SendEmailAsync(user.Email, subject, message);

                // Lưu tạm ID để chuyển sang trang xác nhận OTP
                TempData["UserId"] = user.Id;
                TempData["RememberMe"] = model.RememberMe;
                TempData["ReturnUrl"] = returnUrl;

                return RedirectToAction("VerifyOtp");
            }

            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
            return View(model);
        }

        // === GIAO DIỆN NHẬP OTP ===
        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyOtp()
        {
            if (TempData["UserId"] == null) return RedirectToAction("Login");
            
            TempData.Keep("UserId");
            TempData.Keep("RememberMe");
            TempData.Keep("ReturnUrl");
            
            return View();
        }

        // === XỬ LÝ NÚT BẤM XÁC NHẬN OTP ===
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string otp)
        {
            var userId = TempData["UserId"]?.ToString();
            var rememberMe = TempData["RememberMe"] as bool? ?? false;
            var returnUrl = TempData["ReturnUrl"]?.ToString();

            if (userId == null) return RedirectToAction("Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");

            // Nhờ Microsoft kiểm tra OTP
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", otp);

            if (isValid)
            {
                await _signInManager.SignInAsync(user, isPersistent: rememberMe);
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                    
                return RedirectToAction("Index", "Curriculum");
            }

            ModelState.AddModelError(string.Empty, "Mã OTP không hợp lệ hoặc đã hết hạn.");
            TempData.Keep("UserId");
            TempData.Keep("RememberMe");
            TempData.Keep("ReturnUrl");
            return View();
        }

        // ==========================================
        // 2. ĐĂNG KÝ
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated) return RedirectToAction("Index", "Curriculum");
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra xem Username/Email đã có ai xài trong DB chưa (tránh mất công gửi mail)
                var existingUser = await _userManager.FindByEmailAsync(model.Email) ?? await _userManager.FindByNameAsync(model.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email hoặc Tên đăng nhập này đã có người sử dụng!");
                    return View(model);
                }

                // 2. Tự tạo mã OTP 6 số ngẫu nhiên
                var otp = new Random().Next(100000, 999999).ToString();

                // 3. Đóng gói Dữ liệu Form + OTP lại, nhét vào RAM (Cache) giữ trong 5 phút
                var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                _cache.Set("Reg_" + model.Email, Tuple.Create(model, otp), cacheOptions);

                // 4. Gửi email
                string subject = "🚀 Mã xác nhận đăng ký - AutoCurriculum";
                string message = $"<h3>Chào mừng {model.Username},</h3>" +
                                 $"<p>Mã OTP đăng ký tài khoản của bạn là: <b style='font-size: 24px; color: blue;'>{otp}</b></p>" +
                                 $"<p>Mã này sẽ tự hủy sau 5 phút. Vui lòng không chia sẻ cho ai!</p>";
                await _emailService.SendEmailAsync(model.Email, subject, message);

                // 5. Đá sang trang nhập OTP, chỉ mang theo mỗi cái Email làm chìa khóa
                TempData["RegEmail"] = model.Email;
                return RedirectToAction("VerifyRegistrationOtp");
            }
            return View(model);
        }

        // === GIAO DIỆN NHẬP OTP KÍCH HOẠT ĐĂNG KÝ ===
        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyRegistrationOtp()
        {
            if (TempData["RegEmail"] == null) return RedirectToAction("Register");
            TempData.Keep("RegEmail");
            return View();
        }

        // === XỬ LÝ NÚT BẤM KÍCH HOẠT TÀI KHOẢN ===
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRegistrationOtp(string otp)
        {
            var email = TempData["RegEmail"]?.ToString();
            if (email == null) return RedirectToAction("Register");

            // 1. Mở RAM (Cache) ra tìm cái hộp chứa dữ liệu của Email này
            if (_cache.TryGetValue("Reg_" + email, out Tuple<RegisterViewModel, string>? cachedData))
            {
                var model = cachedData!.Item1; // Lấy lại form đăng ký
                var cachedOtp = cachedData.Item2; // Lấy lại cái OTP gốc

                // 2. So sánh OTP
                if (otp == cachedOtp)
                {
                    // === MÃ ĐÚNG 100% -> BÂY GIỜ MỚI CHÍNH THỨC LƯU VÀO DATABASE ===
                    var user = new ApplicationUser {
                        UserName = model.Username,
                        Email = model.Email,
                        FullName = model.Username,
                        EmailConfirmed = true // Đánh dấu đã xác minh mail luôn
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        _cache.Remove("Reg_" + email); // Dọn rác trong RAM
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Index", "Curriculum");
                    }
                    
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Mã OTP không chính xác.");
                }
            }
            else
            {
                // Nếu vượt quá 5 phút, Cache tự bốc hơi, sẽ rớt xuống đây
                ModelState.AddModelError(string.Empty, "Mã OTP đã hết hạn. Vui lòng quay lại trang Đăng ký.");
            }

            TempData.Keep("RegEmail");
            return View();
        }

        // ==========================================
        // 3. ĐĂNG XUẤT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Curriculum");
        }
        // 1. Trang nhập Email yêu cầu cấp lại mật khẩu
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("Email", "Email này chưa được đăng ký trong hệ thống!");
                    return View(model); 
                }

                try 
                {
                    // 1. TẠO MÃ OTP 6 SỐ (Thay thế cho cái Token dài ngoằng cũ)
                    var otp = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

                    // 2. Gửi Email
                    await _emailService.SendEmailAsync(model.Email, "Mã OTP Đặt lại mật khẩu", $"Mã xác nhận 6 số của bạn là: {otp}");

                    // 3. Chuyển sang trang nhập OTP
                    return RedirectToAction("ResetPassword", "Account", new { email = model.Email });
                }
                catch (Exception ex)
                {
                    // NẾU GMAIL BỊ LỖI -> BẮT LỖI TẠI ĐÂY, KHÔNG CHO SẬP SERVER NỮA!
                    ModelState.AddModelError(string.Empty, "Hệ thống gửi mail đang gặp sự cố: " + ex.Message);
                    return View(model);
                }
            }
            return View(model);
        }

        // 2. Trang nhập mã OTP và Mật khẩu mới
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email) => View(new ResetPasswordViewModel { Email = email });

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            // Nếu không tìm thấy user, quay lại Login để bảo mật thông tin
            if (user == null) return RedirectToAction("Login");

            // 1. Kiểm tra mã OTP 6 số có khớp không
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", model.Token);

            if (isValid)
            {
                // 2. Xóa mật khẩu cũ và đặt mật khẩu mới
                await _userManager.RemovePasswordAsync(user);
                var addResult = await _userManager.AddPasswordAsync(user, model.NewPassword);

                if (addResult.Succeeded)
                {
                    // === THỰC HIỆN ĐĂNG NHẬP LUÔN CHO USER ===
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // === CHUYỂN HƯỚNG THẲNG VỀ INDEX CỦA CURRICULUM ===
                    return RedirectToAction("Index", "Curriculum"); 
                }
                
                foreach (var error in addResult.Errors) ModelState.AddModelError("", error.Description);
            }
            else
            {
                ModelState.AddModelError("", "Mã xác nhận OTP không chính xác hoặc đã hết hạn.");
            }

            return View(model);
        }

        [HttpGet] [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet] [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();
    }
    
}