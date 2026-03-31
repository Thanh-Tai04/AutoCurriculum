using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Cần dùng UserManager để check Role
using AutoCurriculum.Models; 
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AutoCurriculum.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly AutoCurriculumDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(AutoCurriculumDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. HIỂN THỊ DANH SÁCH NGƯỜI DÙNG
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                                .OrderByDescending(u => u.Email) 
                                .ToListAsync();

            return View(users);
        }

        // 2. XỬ LÝ NÚT KHÓA / MỞ KHÓA TÀI KHOẢN
        [HttpPost]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var currentUser = await _userManager.GetUserAsync(User); 
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng này!";
                return RedirectToAction("Index");
            }

            // ═══ BẢO VỆ ADMIN ═══
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["ErrorMessage"] = "Không được phép khóa tài khoản có quyền Quản trị viên!";
                return RedirectToAction("Index");
            }

            if (user.Id == currentUser?.Id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình!";
                return RedirectToAction("Index");
            }

            // Kiểm tra trạng thái khóa
            bool isLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;

            if (isLocked)
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
            }
            else
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }
            
            await _userManager.UpdateAsync(user);
            string actionName = isLocked ? "mở khóa" : "khóa";
            TempData["Message"] = $"Đã {actionName} tài khoản {user.Email} thành công!";
            
            return RedirectToAction("Index");
        }
    }
}