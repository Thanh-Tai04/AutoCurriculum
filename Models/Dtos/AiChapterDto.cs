namespace AutoCurriculum.ViewModels
{
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