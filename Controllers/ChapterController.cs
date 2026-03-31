using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Services.Interfaces;
using AutoCurriculum.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace AutoCurriculum.Controllers
{
    [Authorize]
    public class ChapterController : Controller
    {
        private readonly ICurriculumService _curriculumService;

        public ChapterController(ICurriculumService curriculumService)
        {
            _curriculumService = curriculumService;
        }

        public IActionResult ChapterDetails(int id)
        {
            var chapter = _curriculumService.GetChapterWithLessons(id);
            if (chapter == null) return NotFound();

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

            return RedirectToAction("Details", "Curriculum", new { id = topicId });
        }

        [HttpPost]
        public IActionResult DeleteChapter(int id)
        {
            var chapter = _curriculumService.GetChapterWithLessons(id);
            if (chapter == null) return NotFound();

            int topicId = chapter.TopicId;
            _curriculumService.DeleteChapter(id);

            return RedirectToAction("Details", "Curriculum", new { id = topicId });
        }
    }
}