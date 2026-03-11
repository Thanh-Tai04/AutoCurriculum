using AutoCurriculum.Models;
using AutoCurriculum.Repositories.Interfaces;

namespace AutoCurriculum.Repositories.Implementations
{
    public class ContentRepository : IContentRepository
    {
        private readonly AutoCurriculumDbContext _context;

        public ContentRepository(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        public List<Content> GetByLesson(int lessonId)
        {
            return _context.Contents
                           .Where(c => c.LessonId == lessonId)
                           .OrderBy(c => c.ContentOrder)
                           .ToList();
        }

        public int CountByLesson(int lessonId)
        {
            return _context.Contents.Count(c => c.LessonId == lessonId);
        }

        public void Add(Content content) => _context.Contents.Add(content);

        public async Task SaveAsync() => await _context.SaveChangesAsync();
    }
}