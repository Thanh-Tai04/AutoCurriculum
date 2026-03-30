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

            // 1. LẤY PROMPT TỪ DATABASE THAY VÌ CODE CỨNG
            var promptConfig = _context.PromptConfigs.FirstOrDefault(p => p.PromptCode == "Generate_Curriculum");
            if (promptConfig == null || string.IsNullOrWhiteSpace(promptConfig.PromptText)) 
            {
                throw new Exception("Hệ thống AI đang tạm bảo trì do thiếu cấu hình câu lệnh. Quản trị viên vui lòng kiểm tra lại bảng Prompt!");
            }
            if (promptConfig == null) 
                throw new Exception("Lỗi: Không tìm thấy cấu hình lệnh Generate_Curriculum trong Database. Vui lòng vào trang Admin để kiểm tra.");

            // 2. TÌM VÀ THAY THẾ CÁC BIẾN BẰNG DỮ LIỆU THẬT
            string prompt = promptConfig.PromptText
                .Replace("{topicName}", topicName)
                .Replace("{sourceUrl}", sourceUrl)
                .Replace("{wikiDescription}", wikiDescription)
                .Replace("{sectionsText}", sectionsText);

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

            // 1. LẤY PROMPT TỪ DATABASE THAY VÌ CODE CỨNG
            var promptConfig = _context.PromptConfigs.FirstOrDefault(p => p.PromptCode == "Generate_Lesson");
            if (promptConfig == null || string.IsNullOrWhiteSpace(promptConfig.PromptText)) 
            {
                throw new Exception("Không thể tạo nội dung bài học do thiếu cấu hình Prompt.");
            }
            if (promptConfig == null) 
                throw new Exception("Lỗi: Không tìm thấy cấu hình lệnh Generate_Lesson trong Database.");

            // 2. TÌM VÀ THAY THẾ CÁC BIẾN BẰNG DỮ LIỆU THẬT
            string prompt = promptConfig.PromptText
                .Replace("{topicName}", topicName)
                .Replace("{chapterOrder}", chapterOrder.ToString())
                .Replace("{chapterTitle}", chapterTitle)
                .Replace("{lessonNumber}", lessonNumber)
                .Replace("{lessonTitle}", lessonTitle);

            var result = await CallGeminiAsync(prompt, lessonTitle, "Generate_Lesson");
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