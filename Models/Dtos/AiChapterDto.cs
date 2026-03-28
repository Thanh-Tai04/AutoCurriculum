namespace AutoCurriculum.ViewModels
{
    // BỔ SUNG CLASS NÀY ĐỂ HỨNG TOÀN BỘ GIÁO TRÌNH + NGUỒN
    public class AiCurriculumDto
    {
        public string TopicName { get; set; }
        public string Description { get; set; }
        public string SourceName { get; set; }
        public string SourceUrl { get; set; }
        public List<AiChapterDto> Chapters { get; set; } = new();
    }

    public class AiChapterDto
    {
        public string ChapterTitle { get; set; }
        public List<AiSectionDto> Sections { get; set; } = new();
    }

    public class AiSectionDto
    {
        public string SectionTitle { get; set; }
        public List<string> Lessons { get; set; } = new();
    }
}