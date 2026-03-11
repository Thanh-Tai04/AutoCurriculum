using AutoCurriculum.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace AutoCurriculum.Services.Implementations
{
    public class GeminiService : IGeminiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private string ApiKey => _configuration["GeminiSettings:ApiKey"] ?? "";
        private const string GeminiModel = "gemini-2.5-flash";

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // ── Tạo Curriculum (Chapter + Lesson) ───────────────────────
        public async Task<List<dynamic>> GenerateCurriculumAsync(string wikiDescription)
        {
            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("Chưa cấu hình Gemini API Key!");

            string prompt = $@"Bạn là một Giáo sư Đại học đa ngành. Dựa vào nội dung nền tảng từ Wikipedia sau:

{wikiDescription}

YÊU CẦU BẮT BUỘC:
1. ĐÓNG VAI CHUYÊN GIA: Tự động phân tích xem chủ đề trên thuộc lĩnh vực nào (Ví dụ: Kinh tế, CNTT, Y học, Lịch sử, Nghệ thuật...).
2. NÂNG CẤP HỌC THUẬT: Hãy biến nội dung này thành một GIÁO TRÌNH BÀI BẢN. Tự bổ sung kiến thức chuyên sâu đặc thù của ngành đó:
   - Nếu là CNTT/Toán: Thêm thuật toán, code, logic lõi.
   - Nếu là Kinh tế/Kinh doanh: Thêm mô hình, case study, framework.
   - Nếu là Khoa học Xã hội/Tâm lý: Thêm các học thuyết, hiệu ứng, nhà tư tưởng lớn.
   - Nếu là Lịch sử/Văn hóa: Thêm bối cảnh, mốc sự kiện chi tiết, phân tích nguyên nhân - kết quả.
3. CẤU TRÚC: Thiết kế TỐI ĐA 5 chương, mỗi chương 3-5 bài học có tính liên kết sư phạm từ cơ bản đến nâng cao.
4. ĐẦU RA: CHỈ trả về MẢNG JSON, KHÔNG dùng markdown ```json, không giải thích, tên chương không để 'Chương 1:':
[
  {{
    ""ChapterTitle"": ""Tên chương 1"",
    ""Lessons"": [""Bài 1"", ""Bài 2""]
  }}
]";

            var rawResult = await CallGeminiAsync(prompt);
            var cleaned = rawResult.Replace("```json", "").Replace("```", "").Trim();

            return JsonConvert.DeserializeObject<List<dynamic>>(cleaned)
                   ?? throw new Exception("AI trả về JSON không hợp lệ.");
        }

        // ── Soạn nội dung bài giảng chi tiết ────────────────────────
        public async Task<string> GenerateLessonContentAsync(string topicName, string chapterTitle, string lessonTitle)
        {
            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("Chưa cấu hình Gemini API Key!");

            string prompt = $@"Bạn là một Giảng viên Đại học xuất sắc. Hãy biên soạn nội dung giảng dạy CHI TIẾT cho bài học sau:

- Nằm trong môn học/chủ đề: {topicName}
- Thuộc chương: {chapterTitle}
- Tên bài học hiện tại: {lessonTitle}

YÊU CẦU BẮT BUỘC:
1. Viết một bài giảng sâu sắc, dễ hiểu, văn phong học thuật nhưng gần gũi.
2. Bắt buộc có ví dụ minh họa thực tế. Nếu là CNTT, bắt buộc có đoạn code mẫu.
3. Trình bày bằng HTML cơ bản (dùng thẻ <h3>, <p>, <ul>, <li>, <strong>, <code>). KHÔNG dùng markdown.
4. KHÔNG trả về JSON. Chỉ trả về trực tiếp đoạn nội dung bài học.";

            var result = await CallGeminiAsync(prompt);
            return result.Replace("```html", "").Replace("```", "").Trim();
        }

        // ── Private helper: gọi Gemini API ──────────────────────────
        private async Task<string> CallGeminiAsync(string prompt)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{GeminiModel}:generateContent?key={ApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var httpContent = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            using var client = _httpClientFactory.CreateClient();

            int maxRetries = 3; // Thử tối đa 3 lần
            int delayMs = 2500; // Nghỉ 2.5 giây giữa mỗi lần thử

            for (int i = 0; i < maxRetries; i++)
            {
                var response = await client.PostAsync(url, httpContent);
                var raw = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var json = JObject.Parse(raw);
                    return json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
                           ?? throw new Exception("Gemini trả về kết quả rỗng.");
                }

                // Nếu gặp lỗi 503 (Quá tải) VÀ chưa hết số lần thử -> Đợi rồi thử lại
                if ((int)response.StatusCode == 503 && i < maxRetries - 1)
                {
                    await Task.Delay(delayMs);
                    continue;
                }

                // Nếu gặp lỗi khác hoặc đã hết quyền thử -> Văng lỗi ra ngoài
                throw new Exception($"Lỗi {response.StatusCode}: {raw}");
            }

            return string.Empty;
        }
    }
}