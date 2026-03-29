using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Services.Interfaces;
using AutoCurriculum.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace AutoCurriculum.Controllers
{
    [Authorize]
    public class LessonController : Controller
    {
        private readonly ICurriculumService _curriculumService;

        public LessonController(ICurriculumService curriculumService)
        {
            _curriculumService = curriculumService;
        }

        public IActionResult LessonDetails(int id)
        {
            var lesson = _curriculumService.GetLessonWithContext(id);
            if (lesson == null) return NotFound();

            var contents = _curriculumService.GetLessonContents(id);

            var viewModel = new LessonDetailViewModel
            {
                LessonId = lesson.LessonId,
                LessonTitle = lesson.LessonTitle,
                LessonOrder = lesson.LessonOrder ?? 0,
                ChapterId = lesson.Chapter.ChapterId,
                ChapterTitle = lesson.Chapter.ChapterTitle,
                ChapterOrder = lesson.Chapter.ChapterOrder ?? 0,
                TopicId = lesson.Chapter.Topic.TopicId,
                TopicName = lesson.Chapter.Topic.TopicName,
                HtmlContents = contents.Select(c => c.ContentText).ToList() 
            };

            return View(viewModel); 
        }

        [HttpPost]
        public IActionResult CreateLesson(int chapterId, string lessonTitle)
        {
            if (!string.IsNullOrEmpty(lessonTitle))
                _curriculumService.CreateLesson(chapterId, lessonTitle);

            var chapter = _curriculumService.GetChapterWithLessons(chapterId);
            if (chapter != null)
            {
                return RedirectToAction("Details", "Curriculum", new { id = chapter.TopicId });
            }

            return RedirectToAction("Index", "Curriculum");
        }

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