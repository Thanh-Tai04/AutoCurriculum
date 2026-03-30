using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoCurriculum.Models; 
using System.Linq;
using System.Threading.Tasks;
using System; // Thêm thư viện này để dùng DateTimeOffset

namespace AutoCurriculum.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class UserController : Controller
    {
        private readonly AutoCurriculumDbContext _context;

        public UserController(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        // 1. HIỂN THỊ DANH SÁCH NGƯỜI DÙNG
        public IActionResult Index()
        {
            // Identity không có cột CreatedAt mặc định, nên ta sẽ sắp xếp theo Email
            var users = _context.Users
                                .OrderBy(u => u.Email) 
                                .ToList();

            return View(users);
        }

        // 2. XỬ LÝ NÚT KHÓA / MỞ KHÓA TÀI KHOẢN
        [HttpPost]
        public async Task<IActionResult> ToggleLock(string userId) // ĐỔI THÀNH kiểu chuỗi 'string' vì Id của Identity là string
        {
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng này!";
                return RedirectToAction("Index");
            }

            // Kiểm tra xem user có đang bị khóa hay không (thời gian mở khóa lớn hơn thời gian hiện tại)
            bool isLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;

            if (isLocked)
            {
                // TIẾN HÀNH MỞ KHÓA
                user.LockoutEnd = null; 
                user.AccessFailedCount = 0; // Reset số lần đăng nhập sai
            }
            else
            {
                // TIẾN HÀNH KHÓA (Khóa vĩnh viễn đến năm 9999)
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }
            
            await _context.SaveChangesAsync();

            string actionName = isLocked ? "mở khóa" : "khóa";
            TempData["Message"] = $"Đã {actionName} tài khoản {user.Email} thành công!";
            
            return RedirectToAction("Index");
        }
    }
}