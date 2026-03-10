using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Services.Interfaces;

namespace AutoCurriculum.Controllers
{
    public class CurriculumController : Controller
    {
        private readonly ICurriculumService _curriculumService;

        public CurriculumController(ICurriculumService curriculumService)
        {
            _curriculumService = curriculumService;
        }

        // ── TOPIC ─────────────────────────────────────────────────────

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            var topics = _curriculumService.GetAllTopics();
            return View(topics);
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
                        date = topic.CreatedAt?.ToString("dd/MM/yyyy HH:mm"),
                        desc = topic.Description
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }

        public IActionResult Details(int id)
        {
            var topic = _curriculumService.GetTopicWithChapters(id);
            if (topic == null) return NotFound();
            return View(topic);
        }

        // ── CHAPTER ───────────────────────────────────────────────────

        public IActionResult ChapterDetails(int id)
        {
            var chapter = _curriculumService.GetChapterWithLessons(id);
            if (chapter == null) return NotFound();
            return View(chapter);
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

            return RedirectToAction("ChapterDetails", new { id = chapterId });
        }

        public IActionResult LessonDetails(int id)
        {
            var lesson = _curriculumService.GetLessonWithContext(id);
            if (lesson == null) return NotFound();

            ViewBag.Contents = _curriculumService.GetLessonContents(id);
            return View(lesson);
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