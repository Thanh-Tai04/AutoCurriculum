using AutoCurriculum.ViewModels;
using AutoCurriculum.Services.Interfaces;
using Newtonsoft.Json.Linq;
using System.Diagnostics; 
using AutoCurriculum.Models;
using Microsoft.AspNetCore.Http; 
using System.Security.Claims;

namespace AutoCurriculum.Services.Implementations
{
    public class WikipediaService : IWikipediaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AutoCurriculumDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly HashSet<string> _excludedHeadings = new()
        {
            "Xem thêm", "Tham khảo", "Liên kết ngoài", "Chú thích", "Thư mục"
        };

        public WikipediaService(IHttpClientFactory httpClientFactory, AutoCurriculumDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _context = context; 
            _httpContextAccessor = httpContextAccessor;
        }

        // THÊM THAM SỐ actionName VÀO ĐÂY
        public async Task<(string ExactTitle, string Summary, List<string> Sections)> GetTopicDataAsync(string topicName, string actionName = "Wikipedia_Crawl")
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

            var log = new SystemLog { 
                Action = actionName, // Gán actionName để phân biệt trên Dashboard
                Keyword = topicName, 
                UserEmail = currentUserEmail, 
                CreatedAt = DateTime.Now 
            };

            try 
            {
                var client = _httpClientFactory.CreateClient("Wikipedia");
                string exactTitle = topicName;
                string summary = "Không tìm thấy thông tin trên Wikipedia.";
                var sections = new List<string>();

                // ── BƯỚC 1: Tìm exact title ──────────────────────────────
                string searchUrl = $"https://vi.wikipedia.org/w/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(topicName)}&utf8=&format=json&srlimit=1";
                var searchResponse = await client.GetAsync(searchUrl);

                if (!searchResponse.IsSuccessStatusCode)
                    throw new Exception("Không thể kết nối Wikipedia.");

                var searchJson = JObject.Parse(await searchResponse.Content.ReadAsStringAsync());
                var searchResults = searchJson["query"]?["search"];
                
                if (searchResults == null || !searchResults.HasValues)
                    throw new Exception("Không tìm thấy kết quả nào khớp với từ khóa.");

                exactTitle = searchResults[0]["title"]?.ToString() ?? topicName;

                // ── BƯỚC 2: Lấy Summary ──────────────────────────────────
                string formattedTitle = exactTitle.Replace(" ", "_");
                string summaryUrl = $"https://vi.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(formattedTitle)}";
                var sumResponse = await client.GetAsync(summaryUrl);
                if (sumResponse.IsSuccessStatusCode)
                {
                    var sumJson = JObject.Parse(await sumResponse.Content.ReadAsStringAsync());
                    summary = sumJson["extract"]?.ToString() ?? summary;
                }

                // ── BƯỚC 3: Lấy Sections ─────────────────────────────────
                string sectionUrl = $"https://vi.wikipedia.org/api/rest_v1/page/mobile-sections/{Uri.EscapeDataString(formattedTitle)}";
                var secResponse = await client.GetAsync(sectionUrl);

                if (secResponse.IsSuccessStatusCode)
                {
                    var secJson = JObject.Parse(await secResponse.Content.ReadAsStringAsync());
                    var sectionTokens = secJson["remaining"]?["sections"];

                    if (sectionTokens != null)
                    {
                        foreach (var sec in sectionTokens)
                        {
                            int tocLevel = sec["toclevel"]?.Value<int>() ?? 0;
                            string heading = sec["line"]?.ToString() ?? "";

                            if (tocLevel > 0 && tocLevel <= 2 && !_excludedHeadings.Contains(heading))
                                sections.Add(heading);
                        }
                    }
                }

                log.Status = "Success";
                
                // GHI LOG MESSAGE TIẾNG VIỆT THEO TỪNG HÀNH ĐỘNG
                if (actionName == "Wikipedia_Preview")
                {
                    log.Message = $"Found exact title: {exactTitle}";
                }
                else if (actionName == "Wikipedia_FetchData")
                {
                    log.Message = $"Fetched curriculum structure: {exactTitle}";
                }
                else
                {
                    log.Message = $"Wikipedia data fetched successfully: {exactTitle}";
                }

                return (exactTitle, summary, sections);
            }
            catch (Exception ex)
            {
                log.Status = "Error";
                log.Message = $"Lỗi khi trích xuất: {ex.Message}";
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