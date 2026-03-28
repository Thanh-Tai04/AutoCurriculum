using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Services.Interfaces;
using AutoCurriculum.ViewModels;
using Rotativa.AspNetCore;
namespace AutoCurriculum.Controllers
{
    public class CurriculumController : Controller
    {
        private readonly ICurriculumService _curriculumService;
        private readonly IWikipediaService _wikiService; // BỔ SUNG

        // BỔ SUNG IWikipediaService vào Constructor
        public CurriculumController(ICurriculumService curriculumService, IWikipediaService wikiService)
        {
            _curriculumService = curriculumService;
            _wikiService = wikiService;
        }

        // ── API KIỂM TRA TRƯỚC (PRE-CHECK) ────────────────────────────
        
        [HttpGet]
        public async Task<IActionResult> PreCheckWiki(string topicName)
        {
            if (string.IsNullOrWhiteSpace(topicName))
                return Json(new { success = false, message = "Vui lòng nhập từ khóa." });

            try
            {
                // Chỉ gọi Wikipedia lấy thông tin, KHÔNG gọi AI
                var (exactTitle, summary, _) = await _wikiService.GetTopicDataAsync(topicName);
                
                return Json(new 
                { 
                    success = true, 
                    exactTitle = exactTitle, 
                    summary = summary 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không tìm thấy trên Wikipedia: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Preview(int id)
        {
            // Lấy dữ liệu y hệt như lúc in PDF
            var topic = _curriculumService.GetTopicWithChapters(id);
            if (topic == null) return NotFound();

            // ĐIỂM KHÁC BIỆT: Trả về View HTML bình thường thay vì gọi ViewAsPdf của Rotativa
            return View("ExportToPdf", topic);
        }

        [HttpGet]
        public IActionResult ExportToPdf(int id)
        {
            var topic = _curriculumService.GetTopicWithChapters(id);
            if (topic == null) return NotFound();

            string cleanFileName = AutoCurriculum.Helpers.StringHelper.ConvertToSlug(topic.TopicName);

            return new ViewAsPdf("ExportToPdf", topic)
            {
                FileName = $"GiaoTrinh_{cleanFileName}.pdf", 
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                
                // 1. CĂN LỀ CHUẨN DNC: Trên 20mm, Phải 20mm, Dưới 20mm, Trái 30mm (để đóng gáy sách)
                PageMargins = new Rotativa.AspNetCore.Options.Margins(20, 20, 20, 30),

                // 2. ĐÁNH SỐ TRANG: Thêm lệnh --footer-center "[page]" vào CustomSwitches
                CustomSwitches = "--disable-smart-shrinking --print-media-type " +
                                "--footer-center \"[page]\" --footer-font-size \"13\" --footer-font-name \"Times New Roman\""
            };
        }

        // ── TOPIC ─────────────────────────────────────────────────────

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index() {
            var topics = _curriculumService.GetAllTopics(); // Lấy từ Database 

            // Chuyển đổi sang ViewModel
            var viewModel = topics.Select(t => new CurriculumIndexViewModel {
                TopicId = t.TopicId,
                TopicName = t.TopicName,
                CreatedAt = t.CreatedAt,
                TotalChapters = t.Chapters?.Count ?? 0
            }).ToList();

            return View(viewModel); // Gửi danh sách ViewModel ra ngoài 
        }

        [HttpPost]
        public async Task<IActionResult> Generate(string topicName)
        {
            if (string.IsNullOrEmpty(topicName))
                return Json(new { success = false, message = "Tên chủ đề không được để trống!" });

            try
            {
                var topic = await _curriculumService.GenerateTopicAsync(topicName);
                return Json(new
                {
                    success = true,
                    message = "Tạo thành công!",
                    data = new
                    {
                        id = topic.TopicId,
                        name = topic.TopicName,
                        createdAt = topic.CreatedAt?.ToString("dd/MM/yyyy HH:mm"), 
                        desc = topic.Description
                    }
                });
            }
            catch (Exception ex)
            {
                // Dùng BadRequest để trình duyệt biết đây là một lỗi
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Details(int id)
        {
            var topic = _curriculumService.GetTopicWithChapters(id);
            if (topic == null) return NotFound();
            return View(topic);
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
        // ── CHAPTER ───────────────────────────────────────────────────

        public IActionResult ChapterDetails(int id)
        {
            var chapter = _curriculumService.GetChapterWithLessons(id);
            if (chapter == null) return NotFound();

            // Mapping sang ViewModel chuyên nghiệp
            var viewModel = new ChapterDetailViewModel
            {
                ChapterId = chapter.ChapterId,
                ChapterTitle = chapter.ChapterTitle,
                ChapterOrder = chapter.ChapterOrder ?? 0,
                TopicName = chapter.Topic?.TopicName ?? "N/A",
                Lessons = chapter.Lessons.Select(l => new LessonItemViewModel
                {
                    LessonId = l.LessonId,
                    LessonTitle = l.LessonTitle,
                    // Giả sử bạn có logic kiểm tra nội dung
                    HasContent = l.Contents != null && l.Contents.Any() 
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult CreateChapter(int topicId, string chapterTitle)
        {
            if (!string.IsNullOrEmpty(chapterTitle))
                _curriculumService.CreateChapter(topicId, chapterTitle);

            return RedirectToAction("Details", new { id = topicId });
        }

        [HttpPost]
        public IActionResult DeleteChapter(int id)
        {
            var chapter = _curriculumService.GetChapterWithLessons(id);
            if (chapter == null) return NotFound();

            int topicId = chapter.TopicId;
            _curriculumService.DeleteChapter(id);

            return RedirectToAction("Details", new { id = topicId });
        }

        // ── LESSON ────────────────────────────────────────────────────

        [HttpPost]
        public IActionResult CreateLesson(int chapterId, string lessonTitle)
        {
            if (!string.IsNullOrEmpty(lessonTitle))
                _curriculumService.CreateLesson(chapterId, lessonTitle);

            // 1. Tìm thông tin Chương (Chapter) để lấy được mã Giáo trình (TopicId)
            // Tùy theo cách bạn viết Service/Repository, hãy gọi hàm tương ứng. 
            // Ví dụ:
            var chapter = _curriculumService.GetChapterWithLessons(chapterId);
            if (chapter != null)
            {
                // 2. Trả người dùng về ĐÚNG trang Chi tiết Giáo trình
                return RedirectToAction("Details", new { id = chapter.TopicId });
            }

            // Phòng hờ nếu lỗi không tìm thấy chương thì về trang danh sách
            return RedirectToAction("Index");
        }

        public IActionResult LessonDetails(int id)
        {
            // 1. Lấy dữ liệu gốc từ Database
            var lesson = _curriculumService.GetLessonWithContext(id);
            if (lesson == null) return NotFound();

            var contents = _curriculumService.GetLessonContents(id);

            // 2. Đóng gói dữ liệu vào Hộp "ViewModel" an toàn và sạch sẽ
            var viewModel = new AutoCurriculum.ViewModels.LessonDetailViewModel
            {
                LessonId = lesson.LessonId,
                LessonTitle = lesson.LessonTitle,
                LessonOrder = lesson.LessonOrder ?? 0,
                ChapterId = lesson.Chapter.ChapterId,
                ChapterTitle = lesson.Chapter.ChapterTitle,
                ChapterOrder = lesson.Chapter.ChapterOrder ?? 0,
                TopicId = lesson.Chapter.Topic.TopicId,
                TopicName = lesson.Chapter.Topic.TopicName,
                
                // Trích xuất lấy đúng cột chữ (ContentText) để nhét vào danh sách
                HtmlContents = contents.Select(c => c.ContentText).ToList() 
            };

            // 3. Truyền cái hộp ViewModel này ra View
            return View(viewModel); 
        }

        // ── LESSON CONTENT ────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> GenerateLessonContent(int lessonId)
        {
            try
            {
                await _curriculumService.GenerateLessonContentAsync(lessonId);
                return Json(new { success = true, message = "Đã soạn xong bài giảng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}