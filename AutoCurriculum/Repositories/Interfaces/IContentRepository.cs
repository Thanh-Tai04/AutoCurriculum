using AutoCurriculum.Models;

namespace AutoCurriculum.Repositories.Interfaces
{
    public interface IContentRepository
    {
        List<Content> GetByLesson(int lessonId);
        int CountByLesson(int lessonId);
        void Add(Content content);
        Task SaveAsync();
    }
}