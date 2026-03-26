using AutoCurriculum.Models;
using System.Threading.Tasks;

namespace AutoCurriculum.Repositories.Interfaces
{
    public interface ISectionRepository
    {
        void Add(Section section);
        void Delete(Section section);
        Section? GetById(int sectionId);
        void Save();
        Task SaveAsync();
    }
}