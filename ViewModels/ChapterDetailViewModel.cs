namespace AutoCurriculum.ViewModels
{
    public class ChapterDetailViewModel
    {
        public int ChapterId { get; set; }
        public string ChapterTitle { get; set; }
        public int ChapterOrder { get; set; }
        public int TopicId { get; set; }
        public string TopicName { get; set; } 
        public List<LessonItemViewModel> Lessons { get; set; } = new();
    }

    public class LessonItemViewModel
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public bool HasContent { get; set; } 
    }
}