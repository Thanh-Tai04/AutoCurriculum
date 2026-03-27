using AutoCurriculum.ViewModels;
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
        public async Task<List<dynamic>> GenerateCurriculumAsync(string wikiDescription, List<string> wikiSections)
{
    if (string.IsNullOrEmpty(ApiKey))
        throw new InvalidOperationException("Chưa cấu hình Gemini API Key!");

    // Xử lý danh sách Section từ Wikipedia thành chuỗi để nhét vào Prompt
    string sectionsText = (wikiSections != null && wikiSections.Any()) 
        ? string.Join("\n- ", wikiSections) 
        : "Không có mục lục tham khảo, hãy tự suy luận cấu trúc phù hợp.";

    string prompt = $@"Bạn là một Giáo sư Đại học đa ngành. Dựa vào nội dung nền tảng và CẤU TRÚC MỤC LỤC từ Wikipedia sau:

[TÓM TẮT NỘI DUNG]:
{wikiDescription}

[MỤC LỤC GỐC TỪ WIKIPEDIA]:
- {sectionsText}

YÊU CẦU BẮT BUỘC:
1. ĐÓNG VAI CHUYÊN GIA: Tự động phân tích xem chủ đề trên thuộc lĩnh vực nào (Kinh tế, CNTT, Y học, Lịch sử...).
2. NÂNG CẤP HỌC THUẬT: Biến nội dung này thành một GIÁO TRÌNH BÀI BẢN. Sử dụng [MỤC LỤC GỐC TỪ WIKIPEDIA] làm khung sườn chính để chia Chương (Chapter) và Mục con (Section). Tự bổ sung kiến thức chuyên sâu đặc thù của ngành đó.
3. CẤU TRÚC: Thiết kế TỐI ĐA 5 chương. Mỗi chương bao gồm các Mục con (Section). Mỗi Mục con chứa 2-4 bài học (Lesson) đi từ cơ bản đến nâng cao.
4. QUY TẮC ĐẶT TÊN (RẤT QUAN TRỌNG): TUYỆT ĐỐI KHÔNG thêm các tiền tố như 'Chương 1:', 'Bài 1:', 'Phần 1.1:', '1.', 'a.' vào đầu tên của ChapterTitle, SectionTitle hay Lessons. Chỉ sinh ra phần nội dung tiêu đề thuần túy.
5. ĐẦU RA: CHỈ trả về MẢNG JSON, KHÔNG dùng thẻ markdown ```json, không giải thích thêm. BẮT BUỘC phải theo đúng cấu trúc 3 cấp độ (Chapter -> Section -> Lesson) dưới đây:
[
  {{
    ""ChapterTitle"": ""Tổng quan và Lịch sử phát triển"",
    ""Sections"": [
      {{
        ""SectionTitle"": ""Khái niệm cốt lõi"",
        ""Lessons"": [""Định nghĩa cơ bản"", ""Ứng dụng và tầm quan trọng""]
      }},
      {{
        ""SectionTitle"": ""Kiến trúc và Mô hình"",
        ""Lessons"": [""Các thành phần chính"", ""Luồng hoạt động thực tế""]
      }}
    ]
  }}
]";

    var rawResult = await CallGeminiAsync(prompt);
    
    // Xử lý chuỗi JSON trả về phòng trường hợp AI vẫn nhét thẻ markdown
    var cleaned = rawResult.Replace("```json", "").Replace("```", "").Trim();

    return JsonConvert.DeserializeObject<List<dynamic>>(cleaned)
           ?? throw new Exception("AI trả về JSON không hợp lệ.");
}

        // ── Soạn nội dung bài giảng chi tiết ────────────────────────
        // ── Soạn nội dung bài giảng chi tiết ────────────────────────
        public async Task<string> GenerateLessonContentAsync(string topicName, int chapterOrder, string chapterTitle, int lessonOrder, string lessonTitle)
        {
            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException("Chưa cấu hình Gemini API Key!");

            // Ghép số Chương và số Bài lại. Ví dụ: Chương 1, Bài 1 -> "1.1"
            string lessonNumber = $"{chapterOrder}.{lessonOrder}";

            string prompt = $@"Bạn là một Giảng viên Đại học xuất sắc. Hãy biên soạn nội dung giảng dạy CHI TIẾT cho bài học sau:

- Nằm trong môn học/chủ đề: {topicName}
- Thuộc Chương {chapterOrder}: {chapterTitle}
- Tên bài học hiện tại: Bài {lessonNumber} - {lessonTitle}

YÊU CẦU BẮT BUỘC:
1. TRÌNH BÀY MỤC LỤC CHUẨN: Sử dụng mã số bài học ({lessonNumber}) làm gốc để đánh số phân cấp cho các nội dung bên trong.
   - Các mục chính (dùng thẻ <h3>) BẮT BUỘC đánh số: {lessonNumber}.1, {lessonNumber}.2, {lessonNumber}.3...
   - Các tiểu mục con (dùng thẻ <h4>) BẮT BUỘC đánh số: {lessonNumber}.1.1, {lessonNumber}.1.2...
2. NỘI DUNG: Viết một bài giảng sâu sắc, dễ hiểu, văn phong học thuật nhưng gần gũi.
3. THỰC TẾ: Bắt buộc có ví dụ minh họa thực tế. Nếu là CNTT, bắt buộc có đoạn code mẫu.
4. ĐỊNH DẠNG: Trình bày bằng HTML cơ bản (dùng thẻ <h3>, <h4>, <p>, <ul>, <li>, <strong>, <code>). KHÔNG dùng markdown.
5. KHÔNG trả về JSON. Chỉ trả về trực tiếp đoạn mã HTML nội dung bài học.";

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

        public async Task<string> ClassifyTopicAsync(string topicName)
{
    if (string.IsNullOrEmpty(ApiKey))
        throw new InvalidOperationException("Chưa cấu hình Gemini API Key!");

    string prompt = $@"Bạn là hệ thống kiểm duyệt tự động cho một ứng dụng tạo giáo trình học tập.
Hãy phân loại từ khóa đầu vào: ""{topicName}"" thành đúng 1 trong 3 nhãn sau (CHỈ trả về tên nhãn, không giải thích):

1. BLOCK: Nội dung 18+, bạo lực, chế tạo vũ khí, hack/crack, lừa đảo, tán gái, game hack, spam, hoặc các ký tự vô nghĩa (như asdfgh, xyz123).
2. WARN: Nội dung không vi phạm pháp luật nhưng hơi lệch khỏi mục đích học tập/kỹ năng chuẩn (ví dụ: cách kiếm tiền nhanh, cờ bạc nhẹ, mẹo vặt mảng xám).
3. ALLOW: Các chủ đề học thuật, lập trình, ngôn ngữ, thiết kế, kỹ năng sống, kỹ năng mềm (như edit video, chơi guitar), nghề nghiệp.

Nhãn kết quả:";

    // Tận dụng lại hàm CallGeminiAsync đã viết sẵn
    var rawResult = await CallGeminiAsync(prompt);
    
    // Chuẩn hóa kết quả trả về
    string result = rawResult.Trim().ToUpper();
    
    if (result.Contains("BLOCK")) return "BLOCK";
    if (result.Contains("WARN")) return "WARN";
    return "ALLOW"; // Mặc định cho phép nếu AI không rõ ràng
}
    }
}