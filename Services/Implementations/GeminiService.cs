using AutoCurriculum.DTOs;
using AutoCurriculum.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Diagnostics; 
using AutoCurriculum.Models;
using System.Security.Claims; 
using Microsoft.EntityFrameworkCore; 

namespace AutoCurriculum.Services.Implementations
{
    public class GeminiService : IGeminiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AutoCurriculumDbContext _context; 
        private const string GeminiModel = "gemini-2.5-flash";
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Đã xóa IConfiguration cho code sạch sẽ
        public GeminiService(IHttpClientFactory httpClientFactory, AutoCurriculumDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _context = context; 
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AiCurriculumDto> GenerateCurriculumAsync(string topicName, string sourceUrl, string wikiDescription, List<string> wikiSections)
        {
            string sectionsText = (wikiSections != null && wikiSections.Any()) 
                ? string.Join("\n- ", wikiSections) 
                : "Không có mục lục tham khảo, hãy tự suy luận cấu trúc phù hợp.";

            // FIX: Dùng await và FirstOrDefaultAsync để tối ưu hiệu suất
            var promptConfig = await _context.PromptConfigs.FirstOrDefaultAsync(p => p.PromptCode == "Generate_Curriculum");
            if (promptConfig == null || string.IsNullOrWhiteSpace(promptConfig.PromptText)) 
            {
                throw new Exception("Hệ thống AI đang tạm bảo trì do thiếu cấu hình câu lệnh. Quản trị viên vui lòng kiểm tra lại bảng Prompt!");
            }

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
            string lessonNumber = $"{chapterOrder}.{lessonOrder}";

            // FIX: Dùng await và FirstOrDefaultAsync để tối ưu hiệu suất
            var promptConfig = await _context.PromptConfigs.FirstOrDefaultAsync(p => p.PromptCode == "Generate_Lesson");
            if (promptConfig == null || string.IsNullOrWhiteSpace(promptConfig.PromptText)) 
            {
                throw new Exception("Không thể tạo nội dung bài học do thiếu cấu hình Prompt.");
            }

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
                currentUserEmail = user.FindFirst(ClaimTypes.Email)?.Value 
                                ?? user.Identity.Name 
                                ?? "User ẩn danh";
            }
            
            var log = new SystemLog { Action = actionName, Keyword = keyword, UserEmail = currentUserEmail, CreatedAt = DateTime.Now };

            try 
            {
                // 1. KÉO API KEY TỪ DATABASE RA Ở ĐÂY
                var activeGeminiKey = await _context.ApiKeys
                    .Where(k => k.Provider == "Gemini AI" && k.IsActive)
                    .Select(k => k.KeyValue)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(activeGeminiKey))
                {
                    throw new Exception("Hệ thống chưa được cấu hình API Key. Quản trị viên vui lòng vào Admin để thêm khóa Gemini AI!");
                }

                // 2. GẮN KEY VỪA LẤY VÀO URL
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{GeminiModel}:generateContent?key={activeGeminiKey}";
                
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