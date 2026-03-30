using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AutoCurriculum.Models;
using System.Linq;
using System.Threading.Tasks;

namespace AutoCurriculum.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CurriculumController : Controller
    {
        private readonly AutoCurriculumDbContext _context;

        public CurriculumController(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Kéo dữ liệu từ bảng AspNetUsers thông qua liên kết User
            var topics = await _context.Topics
                .Include(t => t.Chapters)
                .Include(t => t.User) // Phải Include User thì mới lấy được Email từ bảng Identity
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(topics);
        }

        // 2. XÓA GIÁO TRÌNH (DỌN RÁC)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic != null)
            {
                // Xóa Topic. Các Chapter và Lesson liên quan sẽ tự bốc hơi nếu SQL có cài Cascade
                _context.Topics.Remove(topic);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn giáo trình '{topic.TopicName}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy giáo trình này trên hệ thống.";
            }

            return RedirectToAction("Index");
        }
    }
}