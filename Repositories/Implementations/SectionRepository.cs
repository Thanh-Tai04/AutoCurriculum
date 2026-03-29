using AutoCurriculum.Models;
using AutoCurriculum.Repositories.Interfaces;
using System.Threading.Tasks;

namespace AutoCurriculum.Repositories.Implementations
{
    public class SectionRepository : ISectionRepository
    {
        private readonly AutoCurriculumDbContext _context; // Thay bằng DbContext của bạn

        public SectionRepository(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        public void Add(Section section)
        {
            _context.Sections.Add(section);
        }

        public void Delete(Section section)
        {
            _context.Sections.Remove(section);
        }

        public Section? GetById(int sectionId)
        {
            return _context.Sections.Find(sectionId);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}