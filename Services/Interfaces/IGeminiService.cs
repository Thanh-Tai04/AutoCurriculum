using AutoCurriculum.ViewModels;
namespace AutoCurriculum.Services.Interfaces
{
    public interface IGeminiService
    {
        /// <summary>
        /// Gọi Gemini để tạo danh sách Chapter + Lesson từ nội dung Wikipedia
        /// Trả về List dynamic với ChapterTitle và Lessons
        /// </summary>
        Task<AiCurriculumDto> GenerateCurriculumAsync(string topicName, string sourceUrl, string wikiDescription, List<string> wikiSections);

        /// <summary>
        /// Gọi Gemini để soạn nội dung bài giảng chi tiết cho một Lesson
        /// Trả về HTML string
        /// </summary>
        Task<string> GenerateLessonContentAsync(string topicName, int chapterOrder, string chapterTitle, int lessonOrder, string lessonTitle);

        // Task<string> ClassifyTopicAsync(string topicName);
    }
}