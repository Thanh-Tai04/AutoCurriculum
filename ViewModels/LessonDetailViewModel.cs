using System.Collections.Generic;

namespace AutoCurriculum.ViewModels
{
    public class LessonDetailViewModel
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public int LessonOrder { get; set; }
        public int ChapterId { get; set; }
        public string ChapterTitle { get; set; }
        public int ChapterOrder { get; set; }
        public int TopicId { get; set; }
        public string TopicName { get; set; }
        public List<string> HtmlContents { get; set; } = new List<string>();
    }
}