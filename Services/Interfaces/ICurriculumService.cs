using AutoCurriculum.Models;

namespace AutoCurriculum.Services.Interfaces
{
    public interface ICurriculumService
    {
        // ── Topic ─────────────────────────────────────────────────────
        List<Topic> GetAllTopics();
        Topic? GetTopicWithChapters(int id);

        /// <summary>Tạo Topic + tự động sinh Chapter/Lesson từ Wiki + Gemini</summary>
        Task<Topic> GenerateTopicAsync(string topicName);

        // ── Chapter ───────────────────────────────────────────────────
        Chapter? GetChapterWithLessons(int id);
        void CreateChapter(int topicId, string chapterTitle);
        void DeleteChapter(int chapterId);

        // ── Lesson ────────────────────────────────────────────────────
        void CreateLesson(int chapterId, string lessonTitle);
        Lesson? GetLessonWithContext(int lessonId);

        // ── Lesson Content ────────────────────────────────────────────
        Task<string> GenerateLessonContentAsync(int lessonId);
        List<Content> GetLessonContents(int lessonId);
    }
}