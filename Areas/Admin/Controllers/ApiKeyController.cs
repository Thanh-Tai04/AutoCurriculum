using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoCurriculum.Models; 
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AutoCurriculum.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ApiKeyController : Controller
    {
        private readonly AutoCurriculumDbContext _context;

        public ApiKeyController(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        // 1. HIỂN THỊ DANH SÁCH API KEY
        public async Task<IActionResult> Index()
        {
            var apiKeys = await _context.ApiKeys
                                .OrderByDescending(k => k.CreatedAt)
                                .ToListAsync();
            return View(apiKeys);
        }

        // 2. THÊM API KEY MỚI 
        [HttpPost]
        public async Task<IActionResult> Create(string provider, string keyValue)
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(keyValue))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin API Key!";
                return RedirectToAction("Index");
            }
            var existingActiveKeys = await _context.ApiKeys
                                            .Where(k => k.Provider == provider && k.IsActive)
                                            .ToListAsync();
            
            // Lặp qua và tắt hết các key đang bật của Gemini
            foreach (var key in existingActiveKeys)
            {
                key.IsActive = false;
            }

            // Thêm Key mới vào và cho nó độc quyền "Hoạt động"
            var newKey = new ApiKey
            {
                Provider = provider, // "Gemini AI" được gửi ngầm từ View
                KeyValue = keyValue,
                IsActive = true, 
                CreatedAt = DateTime.Now
            };

            _context.ApiKeys.Add(newKey);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đã thêm thành công và tự động kích hoạt API Key mới!";
            return RedirectToAction("Index");
        }

        // 3. BẬT/TẮT API KEY TỪ LỊCH SỬ
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var keyToToggle = await _context.ApiKeys.FindAsync(id);
            if (keyToToggle == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy API Key này!";
                return RedirectToAction("Index");
            }

            // Nếu người dùng đang muốn BẬT key này lên
            if (!keyToToggle.IsActive)
            {
                // Tắt tất cả các key khác cùng loại (Gemini AI) đi
                var otherActiveKeys = await _context.ApiKeys
                                                .Where(k => k.Provider == keyToToggle.Provider && k.Id != id && k.IsActive)
                                                .ToListAsync();
                foreach (var key in otherActiveKeys)
                {
                    key.IsActive = false;
                }
                
                keyToToggle.IsActive = true;
                TempData["Message"] = "Đã chuyển đổi Key thành công. Các Key cũ đã được tự động tạm ngưng!";
            }
            // Nếu người dùng đang muốn TẮT key này đi
            else
            {
                keyToToggle.IsActive = false;
                TempData["Message"] = "Đã tạm ngưng Key. LƯU Ý: Hiện tại hệ thống không có Key Gemini nào hoạt động!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // 4. XÓA API KEY
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var key = await _context.ApiKeys.FindAsync(id);
            if (key == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy API Key để xóa!";
                return RedirectToAction("Index");
            }

            if (key.IsActive)
            {
                TempData["ErrorMessage"] = "Không thể xóa API Key đang ở trạng thái Hoạt động. Vui lòng tạm ngưng trước khi xóa!";
                return RedirectToAction("Index");
            }

            _context.ApiKeys.Remove(key);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đã xóa vĩnh viễn API Key khỏi lịch sử!";
            return RedirectToAction("Index");
        }
    }
}