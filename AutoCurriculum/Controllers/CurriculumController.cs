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
        private readonly IConfiguration _configuration;

        public CurriculumController(AutoCurriculumDbContext context, IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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
                string exactTitle = topicName;

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", "AutoCurriculumApp/1.0 (contact@example.com)");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                try
                {
                    // ==========================================
                    // BƯỚC 1: SEARCH ĐỂ LẤY EXACT TITLE
                    // ==========================================
                    string searchUrl = $"https://vi.wikipedia.org/w/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(topicName)}&utf8=&format=json&srlimit=1";
                    var searchResponse = await client.GetAsync(searchUrl);

                    if (searchResponse.IsSuccessStatusCode)
                    {
                        var searchJsonString = await searchResponse.Content.ReadAsStringAsync();
                        var searchJson = JObject.Parse(searchJsonString);
                        var searchResults = searchJson["query"]?["search"];

                        if (searchResults != null && searchResults.HasValues)
                        {
                            exactTitle = searchResults[0]["title"]?.ToString() ?? topicName;
                        }
                        else
                        {
                            throw new Exception("Không tìm thấy kết quả nào khớp với từ khóa.");
                        }
                    }

                    // ==========================================
                    // BƯỚC 2: LẤY SUMMARY & SECTIONS
                    // ==========================================
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

                    // Lấy Sections
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

                if (filteredChapters.Count > 0)
                {
                    wikiDescription += "\n\n📌 [KHUNG CHƯƠNG TỪ WIKIPEDIA]: \n- " + string.Join("\n- ", filteredChapters);
                }

                // ==========================================
                // BƯỚC 2.5: TÍCH HỢP GEMINI AI (TÙY CHỌN)
                // ==========================================
                string aiJsonResult = "";
                List<dynamic> aiChapters = new List<dynamic>(); // Lưu kết quả parse JSON từ AI

                string apiKey = _configuration["GeminiSettings:ApiKey"];

                if (!string.IsNullOrEmpty(apiKey))
                {
                    string prompt = $@"Bạn là một Giáo sư Đại học đa ngành. Dựa vào nội dung nền tảng từ Wikipedia sau:

{wikiDescription}

YÊU CẦU BẮT BUỘC:
1. ĐÓNG VAI CHUYÊN GIA: Tự động phân tích xem chủ đề trên thuộc lĩnh vực nào (Ví dụ: Kinh tế, CNTT, Y học, Lịch sử, Nghệ thuật...).
2. NÂNG CẤP HỌC THUẬT: Wikipedia thường mô tả quá chung chung. Hãy biến nội dung này thành một GIÁO TRÌNH BÀI BẢN. Bạn BẮT BUỘC phải tự bổ sung các kiến thức chuyên sâu đặc thù của ngành đó dù Wikipedia không nhắc tới chi tiết. Cụ thể:
   - Nếu là CNTT/Toán: Thêm thuật toán, code, logic lõi.
   - Nếu là Kinh tế/Kinh doanh: Thêm mô hình, case study, khung phân tích (framework).
   - Nếu là Khoa học Xã hội/Tâm lý: Thêm các học thuyết, hiệu ứng, nhà tư tưởng lớn.
   - Nếu là Lịch sử/Văn hóa: Thêm bối cảnh, mốc sự kiện chi tiết, phân tích nguyên nhân - kết quả.
3. CẤU TRÚC: Thiết kế TỐI ĐA 5 chương (Chapter), mỗi chương 3-5 bài học (Lesson) có tính liên kết sư phạm từ cơ bản đến nâng cao.
4. ĐẦU RA: CHỈ trả về KẾT QUẢ DUY NHẤT dưới dạng MẢNG JSON, KHÔNG dùng markdown ```json, không giải thích:
[
  {{
    ""ChapterTitle"": ""Tên chương 1"",
    ""Lessons"": [""Bài 1"", ""Bài 2""]
  }}
]";

                    string geminiUrl =
$"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                    var requestBody = new
                    {
                        contents = new[]
                        {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
                    };

                    var content = new StringContent(
                        JsonConvert.SerializeObject(requestBody),
                        Encoding.UTF8,
                        "application/json"
                    );

                    try
                    {
                        var aiResponse = await client.PostAsync(geminiUrl, content);
                        var rawResponse = await aiResponse.Content.ReadAsStringAsync();

                        if (aiResponse.IsSuccessStatusCode)
                        {
                            var aiData = JObject.Parse(rawResponse);
                            aiJsonResult = aiData["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";

                            // Dọn dẹp kết quả
                            aiJsonResult = aiJsonResult.Replace("```json", "").Replace("```", "").Trim();

                            // Parse JSON thành object để lưu vào DB
                            try
                            {
                                aiChapters = JsonConvert.DeserializeObject<List<dynamic>>(aiJsonResult);
                            }
                            catch
                            {
                                aiJsonResult = "[Lỗi: AI trả về JSON không hợp lệ]\n" + aiJsonResult;
                            }
                        }
                        else
                        {
                            aiJsonResult = $"[Lỗi API {aiResponse.StatusCode}]\n{rawResponse}";
                        }
                    }
                    catch (Exception ex)
                    {
                        aiJsonResult = $"[Lỗi gọi AI: {ex.Message}]";
                    }

                    wikiDescription += "\n\n🤖 [KẾT QUẢ AI]: \n" + aiJsonResult;
                }
                else
                {
                    wikiDescription += "\n\n⚠️ [AI chưa được cấu hình - Bỏ qua bước này]";
                }

                // ==========================================
                // BƯỚC 3: LƯU VÀO DATABASE
                // ==========================================
                var newTopic = new Topic
                {
                    TopicName = exactTitle,
                    Description = wikiDescription,
                    CreatedAt = DateTime.Now
                };

                _context.Topics.Add(newTopic);
                await _context.SaveChangesAsync();

                // ==========================================
                // BƯỚC 4: TỰ ĐỘNG TẠO CHAPTER & LESSON TỪ AI (NẾU CÓ)
                // ==========================================
                if (aiChapters != null && aiChapters.Count > 0)
                {
                    int chapterOrder = 1;
                    foreach (var aiChap in aiChapters)
                    {
                        string chapterTitle = aiChap.ChapterTitle?.ToString();
                        if (string.IsNullOrEmpty(chapterTitle)) continue;

                        var newChapter = new Chapter
                        {
                            TopicId = newTopic.TopicId,
                            ChapterTitle = chapterTitle,
                            ChapterOrder = chapterOrder++
                        };

                        _context.Chapters.Add(newChapter);
                        await _context.SaveChangesAsync(); // Lưu để lấy ChapterId

                        // Tạo Lessons cho Chapter này
                        if (aiChap.Lessons != null)
                        {
                            int lessonOrder = 1;
                            foreach (var lessonTitle in aiChap.Lessons)
                            {
                                var newLesson = new Lesson
                                {
                                    ChapterId = newChapter.ChapterId,
                                    LessonTitle = lessonTitle.ToString(),
                                    LessonOrder = lessonOrder++
                                };
                                _context.Lessons.Add(newLesson);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }

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