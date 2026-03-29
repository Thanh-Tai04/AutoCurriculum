namespace AutoCurriculum.DTOs
{
    // 1. CẤP ĐỘ CAO NHẤT: Hứng toàn bộ thông tin Giáo trình và Nguồn Wiki
    public class AiCurriculumDto
    {
        public string TopicName { get; set; }
        public string Description { get; set; }
        public string SourceName { get; set; } // Hứng tên bài Wiki để làm nguồn
        public string SourceUrl { get; set; }  // Hứng link Wiki
        
        public List<AiChapterDto> Chapters { get; set; } = new();
    }

    // 2. CẤP ĐỘ 2: Hứng thông tin từng Chương
    public class AiChapterDto
    {
        public string ChapterTitle { get; set; }
        
        public List<AiSectionDto> Sections { get; set; } = new();
    }

    // 3. CẤP ĐỘ 3: Hứng thông tin từng Mục con và Danh sách Bài học
    public class AiSectionDto
    {
        public string SectionTitle { get; set; }
        
        // Hứng trực tiếp mảng chuỗi (tên bài học) từ JSON
        public List<string> Lessons { get; set; } = new();
    }
}