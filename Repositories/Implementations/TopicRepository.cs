using AutoCurriculum.Models;
using AutoCurriculum.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoCurriculum.Repositories.Implementations
{
    public class TopicRepository : ITopicRepository
    {
        private readonly AutoCurriculumDbContext _context;

        public TopicRepository(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả Topic, sắp xếp mới nhất lên đầu
        public List<Topic> GetAllOrderedByDate()
        {
            return _context.Topics
                           .OrderByDescending(t => t.CreatedAt)
                           .ToList();
        }

        // Lấy Topic theo ID, kèm danh sách Chapters
        public Topic? GetByIdWithChapters(int id)
        {
            return _context.Topics
                    .Include(t => t.Source)
                        .Include(t => t.Chapters)
                            .ThenInclude(c => c.Lessons)
                                .ThenInclude(l => l.Contents) // PHẢI CÓ DÒNG NÀY THÌ MỚI IN RA CHỮ ĐƯỢC
                        .FirstOrDefault(t => t.TopicId == id);
        }

        public void Add(Topic topic) => _context.Topics.Add(topic);

        public void Save() => _context.SaveChanges();

        public async Task SaveAsync() => await _context.SaveChangesAsync();

        public void Delete(Topic topic)
        {
            _context.Topics.Remove(topic);
        }
    }
}