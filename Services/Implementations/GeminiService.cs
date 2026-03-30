using AutoCurriculum.DTOs;
using AutoCurriculum.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Diagnostics; // Thêm để dùng Stopwatch
using AutoCurriculum.Models;
using System.Security.Claims; // Thêm để dùng SystemLog

namespace AutoCurriculum.Services.Implementations
{
    public class GeminiService : IGeminiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly AutoCurriculumDbContext _context; // Thêm DbContext

        private string ApiKey => _configuration["GeminiSettings:ApiKey"] ?? "";
        private const string GeminiModel = "gemini-2.5-flash";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, AutoCurriculumDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _context = context; // Inject DbContext
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AiCurriculumDto> GenerateCurriculumAsync(string topicName, string sourceUrl, string wikiDescription, List<string> wikiSections)
        {
            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("Chưa cấu hình Gemini API Key!");

            string sectionsText = (wikiSections != null && wikiSections.Any()) 
                ? string.Join("\n- ", wikiSections) 
                : "Không có mục lục tham khảo, hãy tự suy luận cấu trúc phù hợp.";

           string prompt = $@"Bạn là một Giáo sư Đại học đa ngành. Dựa vào nội dung nền tảng và CẤU TRÚC MỤC LỤC từ Wikipedia sau:

            [CHỦ ĐỀ]: {topicName}
            https://hacom.vn/nguon-may-tinh: {sourceUrl}
            [TÓM TẮT NỘI DUNG]:
            {wikiDescription}

            [MỤC LỤC GỐC TỪ WIKIPEDIA]:
            - {sectionsText}

            YÊU CẦU BẮT BUỘC:
            1. ĐÓNG VAI CHUYÊN GIA: Tự động phân tích xem chủ đề trên thuộc lĩnh vực nào (Kinh tế, CNTT, Y học, Lịch sử...).
            2. NÂNG CẤP HỌC THUẬT: Biến nội dung này thành một GIÁO TRÌNH BÀI BẢN. Sử dụng [MỤC LỤC GỐC TỪ WIKIPEDIA] làm khung sườn chính để chia Chương (Chapter) và Mục con (Section). Tự bổ sung kiến thức chuyên sâu đặc thù của ngành đó.
            3. CẤU TRÚC: Thiết kế TỐI ĐA 5 chương. Mỗi chương bao gồm các Mục con (Section). Mỗi Mục con chứa 2-4 bài học (Lesson) đi từ cơ bản đến nâng cao.
            4. QUY TẮC ĐẶT TÊN (RẤT QUAN TRỌNG): TUYỆT ĐỐI KHÔNG thêm các tiền tố như 'Chương 1:', 'Bài 1:', 'Phần 1.1:', '1.', 'a.' vào đầu tên của ChapterTitle, SectionTitle hay Lessons. Chỉ sinh ra phần nội dung tiêu đề thuần túy.
            5. ĐẦU RA: CHỈ trả về MỘT OBJECT JSON DUY NHẤT, KHÔNG dùng thẻ markdown ```json, không giải thích thêm. BẮT BUỘC phải theo đúng cấu trúc dưới đây:
            {{
                ""TopicName"": ""{topicName}"",
                ""Description"": ""Tóm tắt mục tiêu của giáo trình này (khoảng 2-3 câu)."",
                ""SourceName"": ""Tên bài viết Wikipedia (Ví dụ: {topicName})"",
                ""SourceUrl"": ""{sourceUrl}"",
                ""Chapters"": [
                {{
                    ""ChapterTitle"": ""Tổng quan và Lịch sử phát triển"",
                    ""Sections"": [
                    {{
                        ""SectionTitle"": ""Khái niệm cốt lõi"",
                        ""Lessons"": [""Định nghĩa cơ bản"", ""Ứng dụng và tầm quan trọng""]
                    }}
                    ]
                }}
                ]
            }}";

            // Truyền thêm topicName để ghi log
            var rawResult = await CallGeminiAsync(prompt, topicName, "Generate_Curriculum");
            
            var cleaned = rawResult.Replace("```json", "").Replace("```", "").Trim();

            return JsonConvert.DeserializeObject<AiCurriculumDto>(cleaned)
                ?? throw new Exception("AI trả về JSON không hợp lệ.");
        }

        public async Task<string> GenerateLessonContentAsync(string topicName, int chapterOrder, string chapterTitle, int lessonOrder, string lessonTitle)
        {
            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("Chưa cấu hình Gemini API Key!");

            string lessonNumber = $"{chapterOrder}.{lessonOrder}";

            string prompt = $@"Bạn là một Giảng viên Đại học biên soạn tài liệu giáo trình. Hãy biên soạn nội dung giảng dạy CHI TIẾT cho bài học sau:

                - Nằm trong môn học/chủ đề: {topicName}
                - Thuộc Chương {chapterOrder}: {chapterTitle}
                - Tên bài học hiện tại: Bài {lessonNumber} - {lessonTitle}

                YÊU CẦU BẮT BUỘC:
                1. TRÌNH BÀY MỤC LỤC CHUẨN: Sử dụng mã số bài học ({lessonNumber}) làm gốc để đánh số phân cấp cho các nội dung bên trong.
                - Các mục chính (dùng thẻ <h3>) BẮT BUỘC đánh số: {lessonNumber}.1, {lessonNumber}.2...
                - Các tiểu mục con (dùng thẻ <h4>) BẮT BUỘC đánh số: {lessonNumber}.1.1, {lessonNumber}.1.2...
                2. NỘI DUNG: Viết một bài giảng sâu sắc, dễ hiểu, văn phong học thuật.
                3. THỰC TẾ: Bắt buộc có ví dụ minh họa thực tế. Nếu là CNTT, bắt buộc có đoạn code mẫu.
                4. ĐỊNH DẠNG: Trình bày bằng HTML cơ bản (dùng thẻ <h3>, <h4>, <p>, <ul>, <li>, <strong>, <code>). KHÔNG dùng markdown.
                5. KHÔNG trả về JSON. Chỉ trả về trực tiếp đoạn mã HTML nội dung bài học.

                // --- 2 ĐIỀU KIỆN MỚI THÊM VÀO ---
                6. TUYỆT ĐỐI KHÔNG CHÀO HỎI: Không được viết các câu mở đầu giao tiếp như 'Chào các em', 'Hôm nay chúng ta sẽ học...', 'Để giúp các em có cái nhìn...'. 
                7. VÀO THẲNG VẤN ĐỀ: Đoạn HTML trả về BẮT BUỘC phải bắt đầu ngay lập tức bằng thẻ <h3> của mục {lessonNumber}.1. Không có bất kỳ đoạn văn <p> nào nằm trước thẻ <h3> đầu tiên này.
                ";
            // Truyền thêm lessonTitle để ghi log
            var result = await CallGeminiAsync(prompt, lessonTitle, "Generate_Lesson" );
            return result.Replace("```html", "").Replace("```", "").Trim();
        }

        private async Task<string> CallGeminiAsync(string prompt, string keyword, string actionName)
        {
            var watch = Stopwatch.StartNew();
            string currentUserEmail = "Khách (Chưa đăng nhập)";
            var user = _httpContextAccessor.HttpContext?.User;
            
            if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
            {
                // Ưu tiên lấy Email từ Claims, nếu không có thì lấy Name
                currentUserEmail = user.FindFirst(ClaimTypes.Email)?.Value 
                                ?? user.Identity.Name 
                                ?? "User ẩn danh";
            }
            var log = new SystemLog { Action = actionName, Keyword = keyword, UserEmail = currentUserEmail, CreatedAt = DateTime.Now };

            try 
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{GeminiModel}:generateContent?key={ApiKey}";
                var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

                using var client = _httpClientFactory.CreateClient();
                int maxRetries = 3;
                int delayMs = 2500;

                for (int i = 0; i < maxRetries; i++)
                {
                    var httpContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, httpContent);
                    var raw = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var json = JObject.Parse(raw);
                        log.Status = "Success";
                        log.Message = "Gemini responded successfully.";
                        return json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
                               ?? throw new Exception("Gemini trả về kết quả rỗng.");
                    }

                    if ((int)response.StatusCode == 503 && i < maxRetries - 1)
                    {
                        await Task.Delay(delayMs);
                        continue;
                    }
                    throw new Exception($"Lỗi {response.StatusCode}: {raw}");
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                log.Status = "Error";
                log.Message = ex.Message;
                throw;
            }
            finally
            {
                watch.Stop();
                log.ExecutionTimeMs = watch.ElapsedMilliseconds;
                _context.SystemLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }
    }
}