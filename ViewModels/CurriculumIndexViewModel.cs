namespace AutoCurriculum.ViewModels
{
    public class CurriculumIndexViewModel
    {
        public int TopicId { get; set; }
        public string TopicName { get; set; }
        
        public DateTime? CreatedAt { get; set; } 
        
        public int TotalChapters { get; set; }
    }
}