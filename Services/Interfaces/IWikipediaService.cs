namespace AutoCurriculum.Services.Interfaces
{
    public interface IWikipediaService
    {
        /// <summary>
        /// Tìm kiếm và trả về: (exactTitle, summary, danh sách section headings)
        /// </summary>
        Task<(string ExactTitle, string Summary, List<string> Sections)> GetTopicDataAsync(string topicName, string actionName = "Wikipedia_Crawl");
    }
}