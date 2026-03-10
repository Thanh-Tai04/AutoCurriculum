using AutoCurriculum.Models;
using AutoCurriculum.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoCurriculum.Repositories.Implementations
{
    public class ChapterRepository : IChapterRepository
    {
        private readonly AutoCurriculumDbContext _context;

        public ChapterRepository(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        // Lấy Chapter theo ID, kèm danh sách Lessons
        public Chapter? GetByIdWithLessons(int id)
        {
            return _context.Chapters
                           .Include(c => c.Lessons)
                           .FirstOrDefault(c => c.ChapterId == id);
        }

        public void Add(Chapter chapter) => _context.Chapters.Add(chapter);

        public void Delete(Chapter chapter) => _context.Chapters.Remove(chapter);

        public void Save() => _context.SaveChanges();

        public async Task SaveAsync() => await _context.SaveChangesAsync();
    }
}