using AutoCurriculum.Models;
using AutoCurriculum.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoCurriculum.Repositories.Implementations
{
    public class LessonRepository : ILessonRepository
    {
        private readonly AutoCurriculumDbContext _context;

        public LessonRepository(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        // Lấy Lesson kèm Chapter -> Topic (dùng cho breadcrumb và GenerateContent)
        public Lesson? GetByIdWithContext(int id)
        {
            return _context.Lessons
                           .Include(l => l.Chapter)
                               .ThenInclude(c => c.Topic)
                           .FirstOrDefault(l => l.LessonId == id);
        }

        public void Add(Lesson lesson) => _context.Lessons.Add(lesson);

        public void Save() => _context.SaveChanges();

        public async Task SaveAsync() => await _context.SaveChangesAsync();
    }
}