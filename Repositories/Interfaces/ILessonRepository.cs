using AutoCurriculum.Models;

namespace AutoCurriculum.Repositories.Interfaces
{
    public interface ILessonRepository
    {
        Lesson? GetByIdWithContext(int id); // Kèm Chapter -> Topic (cho breadcrumb)
        void Add(Lesson lesson);
        void Save();
        Task SaveAsync();
    }
}