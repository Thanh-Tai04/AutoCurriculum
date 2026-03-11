using AutoCurriculum.Models;

namespace AutoCurriculum.Repositories.Interfaces
{
    public interface ITopicRepository
    {
        List<Topic> GetAllOrderedByDate();
        Topic? GetByIdWithChapters(int id);
        void Add(Topic topic);
        void Save();
        Task SaveAsync();
    }
}