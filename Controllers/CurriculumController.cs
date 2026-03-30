using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Services.Interfaces;
using AutoCurriculum.ViewModels;
using AutoCurriculum.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AutoCurriculum.Controllers
{
    [Authorize] // Bảo vệ toàn bộ Controller
    public class CurriculumController : Controller
    {
        private readonly ICurriculumService _curriculumService;
        private readonly IWikipediaService _wikiService; 
        private readonly AutoCurriculumDbContext _context;

        public CurriculumController(ICurriculumService curriculumService, IWikipediaService wikiService, AutoCurriculumDbContext context)
        {
            _curriculumService = curriculumService;
            _wikiService = wikiService;
            _context = context;
        }

        // ── 1. GIAO DIỆN CHÍNH & QUẢN LÝ TOPIC ──

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index() 
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var topics = _curriculumService.GetAllTopics()
                                        .Where(t => t.UserId == userId)
                                        .ToList();

            var viewModel = topics.Select(t => new CurriculumIndexViewModel {
                TopicId = t.TopicId,
                TopicName = t.TopicName,
                CreatedAt = t.CreatedAt,
                TotalChapters = t.Chapters?.Count ?? 0
            }).ToList();

            return View(viewModel);
        }

        public IActionResult Details(int id)
        {
            var topic = _curriculumService.GetTopicWithChapters(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (topic == null || topic.UserId != userId) return Forbid(); 

            return View(topic);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Topic model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.UserId = userId; 
            
            _context.Topics.Add(model);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Tạo giáo trình thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteTopic(int id)
        {
            try
            {
                _curriculumService.DeleteTopic(id);
                return Json(new { success = true, message = "Đã xóa thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        // ── 2. AI & WIKIPEDIA API ──

        [HttpGet]
        public async Task<IActionResult> PreCheckWiki(string topicName)
        {
            if (string.IsNullOrWhiteSpace(topicName))
                return Json(new { success = false, message = "Vui lòng nhập từ khóa." });

            try
            {
                // ĐÃ THÊM "Wikipedia_Preview" VÀO ĐÂY
                var (exactTitle, summary, _) = await _wikiService.GetTopicDataAsync(topicName, "Wikipedia_Preview");
                return Json(new { success = true, exactTitle = exactTitle, summary = summary });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không tìm thấy trên Wikipedia: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Generate(string topicName)
        {
            if (string.IsNullOrEmpty(topicName))
                return Json(new { success = false, message = "Tên chủ đề không được để trống!" });

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var topic = await _curriculumService.GenerateTopicAsync(topicName);

                topic.UserId = userId;
                _context.Topics.Update(topic);
                await _context.SaveChangesAsync();

                return Json(new {
                    success = true, message = "Tạo thành công!",
                    data = new {
                        id = topic.TopicId, name = topic.TopicName,
                        createdAt = topic.CreatedAt?.ToString("dd/MM/yyyy HH:mm"), desc = topic.Description
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}