using AutoCurriculum.ViewModels;
using AutoCurriculum.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace AutoCurriculum.Services.Implementations
{
    public class WikipediaService : IWikipediaService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // Các heading không liên quan đến nội dung học thuật
        private static readonly HashSet<string> _excludedHeadings = new()
        {
            "Xem thêm", "Tham khảo", "Liên kết ngoài", "Chú thích", "Thư mục"
        };

        public WikipediaService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(string ExactTitle, string Summary, List<string> Sections)> GetTopicDataAsync(string topicName)
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
            Console.WriteLine(searchResults);
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

            return (exactTitle, summary, sections);
        }
    }
}