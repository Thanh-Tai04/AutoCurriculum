using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Models;
using Microsoft.EntityFrameworkCore; // Thêm dòng này để dùng các hàm xử lý data

namespace AutoCurriculum.Controllers
{
    public class CurriculumController : Controller
    {
        private readonly AutoCurriculumDbContext _context;

        public CurriculumController(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        // GET: Hiển thị trang nhập và danh sách chủ đề cũ
        public IActionResult Index()
        {
            // Lấy danh sách Topic từ Database, sắp xếp mới nhất lên đầu
            var topics = _context.Topics
                                 .OrderByDescending(t => t.CreatedAt) // Sắp xếp giảm dần theo ngày tạo
                                 .ToList();

            return View(topics); // Truyền dữ liệu sang View
        }

        // POST: Xử lý khi bấm nút "Tạo Ngay"
        [HttpPost]
        public IActionResult Generate(string topicName)
        {
            if (!string.IsNullOrEmpty(topicName))
            {
                // 1. Tạo đối tượng Topic mới
                var newTopic = new Topic
                {
                    TopicName = topicName,
                    Description = "Đang chờ tạo nội dung...", // Tạm thời để trống
                    CreatedAt = DateTime.Now
                };

                // 2. Thêm vào Database
                _context.Topics.Add(newTopic);
                _context.SaveChanges(); // Lệnh này sẽ chạy INSERT INTO Topics...

                TempData["Message"] = "Đã lưu chủ đề thành công!";
            }

            // 3. Quay lại trang chủ để thấy dữ liệu mới
            return RedirectToAction("Index");
        }
        // 1. GET: Xem chi tiết Topic + Danh sách Chapter bên trong
        public IActionResult Details(int id)
        {
            // Tìm chủ đề theo ID, đồng thời lấy luôn danh sách Chapters của nó (dùng Include)
            var topic = _context.Topics
                                .Include(t => t.Chapters) // Cần using Microsoft.EntityFrameworkCore;
                                .FirstOrDefault(t => t.TopicId == id);
            if (topic == null)
            {
                return NotFound(); // Nếu không thấy thì báo lỗi
            }
            return View(topic);
        }
        // GET: Xem chi tiết một Chương (để liệt kê các Bài học)
        public IActionResult ChapterDetails(int id)
        {
            var chapter = _context.Chapters
                                  .Include(c => c.Lessons) // Kèm theo danh sách bài học
                                  .FirstOrDefault(c => c.ChapterId == id);

            if (chapter == null) return NotFound();

            return View(chapter);
        }
        // 2. POST: Thêm nhanh một chương mới vào Topic này
        [HttpPost]
        public IActionResult CreateChapter(int topicId, string chapterTitle)
        {
            if (!string.IsNullOrEmpty(chapterTitle))
            {
                var newChapter = new Chapter
                {
                    TopicId = topicId,// Gắn chương này vào Topic đang xem
                    ChapterTitle = chapterTitle,
                    ChapterOrder = 1 // Tạm thời để mặc định, sau này sẽ làm logic tự tăng
                };
                _context.Chapters.Add(newChapter);
                _context.SaveChanges();
            }
            // Load lại trang chi tiết của Topic đó
            return RedirectToAction("Details", new { id = topicId});
        }
        // POST: Xóa một chương theo ID
        [HttpPost]
        public IActionResult DeleteChapter(int id)
        {
            // Tìm chương cần xóa
            var chapter = _context.Chapters.Find(id);

            if (chapter != null)
            {
                int topicId = chapter.TopicId; // Lưu lại ID Topic để tí nữa quay lại đúng trang cũ

                _context.Chapters.Remove(chapter); // Xóa khỏi bộ nhớ đệm
                _context.SaveChanges(); // Lệnh DELETE gửi xuống SQL

                // Quay lại trang chi tiết của Topic
                return RedirectToAction("Details", new { id = topicId });
            }

            return NotFound();
        }
        // POST: Thêm bài học mới vào Chương
        [HttpPost]
        public IActionResult CreateLesson(int chapterId, string lessonTitle)
        {
            if (!string.IsNullOrEmpty(lessonTitle))
            {
                var newLesson = new Lesson
                {
                    ChapterId = chapterId,
                    LessonTitle = lessonTitle,
                    LessonOrder = 1 // Logic sắp xếp để sau
                };

                _context.Lessons.Add(newLesson);
                _context.SaveChanges();
            }
            return RedirectToAction("ChapterDetails", new { id = chapterId });
        }
    }
}