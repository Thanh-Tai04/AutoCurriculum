using AutoCurriculum.Models;

namespace AutoCurriculum.Repositories.Interfaces
{
    public interface IChapterRepository
    {
        Chapter? GetByIdWithLessons(int id);
        void Add(Chapter chapter);
        void Delete(Chapter chapter);
        void Save();
        Task SaveAsync();
    }
}