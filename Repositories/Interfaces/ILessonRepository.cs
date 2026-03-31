using AutoCurriculum.Models;

namespace AutoCurriculum.Repositories.Interfaces
{
    public interface ILessonRepository
    {
        Lesson? GetByIdWithContext(int id); 
        void Add(Lesson lesson);
        void Save();
        Task SaveAsync();
    }
}