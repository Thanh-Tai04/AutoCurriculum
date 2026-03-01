using AutoCurriculum.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Thêm dòng này để dùng các hàm xử lý data
using Newtonsoft.Json.Linq; // Để đọc JSON
using System;
using System.Net.Http; // Để gọi web
using System.Text;
using Newtonsoft.Json;

namespace AutoCurriculum.Controllers
{
    public class CurriculumController : Controller
    {
        private readonly AutoCurriculumDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public CurriculumController(AutoCurriculumDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // GET: Hiển thị trang nhập và danh sách chủ đề cũ
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
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
        public async Task<IActionResult> Generate(string topicName)
        {
            if (string.IsNullOrEmpty(topicName))
            {
                return Json(new { success = false, message = "Tên chủ đề không được để trống!" });
            }

            try
            {
                string wikiDescription = "Không tìm thấy thông tin trên Wikipedia.";
                List<string> filteredChapters = new List<string>();
                string exactTitle = topicName; // Mặc định lấy tên người dùng nhập

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", "AutoCurriculumApp/1.0 (contact@example.com)");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                try
                {
                    // ==========================================
                    // BƯỚC 1: SEARCH ĐỂ LẤY EXACT TITLE (Tên chuẩn)
                    // ==========================================
                    string searchUrl = $"https://vi.wikipedia.org/w/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(topicName)}&utf8=&format=json&srlimit=1";
                    var searchResponse = await client.GetAsync(searchUrl);

                    if (searchResponse.IsSuccessStatusCode)
                    {
                        var searchJsonString = await searchResponse.Content.ReadAsStringAsync();
                        var searchJson = JObject.Parse(searchJsonString);

                        // Trích xuất kết quả đầu tiên (Top 1)
                        var searchResults = searchJson["query"]?["search"];
                        if (searchResults != null && searchResults.HasValues)
                        {
                            exactTitle = searchResults[0]["title"]?.ToString() ?? topicName;
                        }
                        else
                        {
                            // Trả về luôn nếu không search ra gì cả
                            throw new Exception("Không tìm thấy kết quả nào khớp với từ khóa.");
                        }
                    }

                    // ==========================================
                    // BƯỚC 2: CÓ TITLE CHUẨN -> GỌI SUMMARY & SECTIONS
                    // ==========================================
                    // Thay khoảng trắng bằng dấu gạch dưới trên Title Chuẩn
                    string formattedTopic = exactTitle.Replace(" ", "_");

                    string summaryUrl = $"https://vi.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(formattedTopic)}";
                    string sectionUrl = $"https://vi.wikipedia.org/api/rest_v1/page/mobile-sections/{Uri.EscapeDataString(formattedTopic)}";

                    // Lấy Summary
                    var sumResponse = await client.GetAsync(summaryUrl);
                    if (sumResponse.IsSuccessStatusCode)
                    {
                        var sumJson = JObject.Parse(await sumResponse.Content.ReadAsStringAsync());
                        wikiDescription = sumJson["extract"]?.ToString() ?? wikiDescription;
                    }

                    // Lấy Sections và Lọc (Rule-based)
                    var secResponse = await client.GetAsync(sectionUrl);
                    if (secResponse.IsSuccessStatusCode)
                    {
                        var secJson = JObject.Parse(await secResponse.Content.ReadAsStringAsync());
                        var sections = secJson["remaining"]?["sections"];
                        if (sections != null)
                        {
                            foreach (var sec in sections)
                            {
                                int tocLevel = sec["toclevel"]?.Value<int>() ?? 0;
                                string heading = sec["line"]?.ToString() ?? "";

                                if (tocLevel > 0 && tocLevel <= 2 &&
                                    !heading.Contains("Xem thêm") &&
                                    !heading.Contains("Tham khảo") &&
                                    !heading.Contains("Liên kết ngoài") &&
                                    !heading.Contains("Chú thích") &&
                                    !heading.Contains("Thư mục"))
                                {
                                    filteredChapters.Add(heading);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    wikiDescription = $"Hệ thống báo: {ex.Message}";
                }

                // Tạm nối danh sách mục lục vào để test
                if (filteredChapters.Count > 0)
                {
                    wikiDescription += "\n\n📌 [KHUNG CHƯƠNG TỪ WIKIPEDIA]: \n- " + string.Join("\n- ", filteredChapters);
                }

                // ==========================================
                // BƯỚC 3: LƯU VÀO DATABASE VỚI TÊN CHUẨN
                // ==========================================
                var newTopic = new Topic
                {
                    TopicName = exactTitle, // Rất hay: Lưu tên chuẩn xác của Wiki vào DB luôn!
                    Description = wikiDescription,
                    CreatedAt = DateTime.Now
                };

                _context.Topics.Add(newTopic);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Tạo thành công!",
                    data = new
                    {
                        id = newTopic.TopicId,
                        name = newTopic.TopicName,
                        date = newTopic.CreatedAt?.ToString("dd/MM/yyyy HH:mm"),
                        desc = wikiDescription
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi Server: " + ex.Message });
            }
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