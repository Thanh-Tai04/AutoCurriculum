using AutoCurriculum.Models;
using AutoCurriculum.ViewModels;
using AutoCurriculum.Repositories.Interfaces;
using AutoCurriculum.Services.Interfaces;
using Newtonsoft.Json.Linq; // Thêm thư viện này để xử lý JSON gọn gàng hơn

namespace AutoCurriculum.Services.Implementations
{
    public class CurriculumService : ICurriculumService
    {
        private readonly ITopicRepository _topicRepo;
        private readonly IChapterRepository _chapterRepo;
        private readonly ISectionRepository _sectionRepo; // BỔ SUNG REPOSITORY CHO SECTION
        private readonly ILessonRepository _lessonRepo;
        private readonly IContentRepository _contentRepo;
        private readonly IWikipediaService _wikiService;
        private readonly IGeminiService _geminiService;

        public CurriculumService(
            ITopicRepository topicRepo,
            IChapterRepository chapterRepo,
            ISectionRepository sectionRepo, // BỔ SUNG VÀO CONSTRUCTOR
            ILessonRepository lessonRepo,
            IContentRepository contentRepo,
            IWikipediaService wikiService,
            IGeminiService geminiService)
        {
            _topicRepo = topicRepo;
            _chapterRepo = chapterRepo;
            _sectionRepo = sectionRepo;
            _lessonRepo = lessonRepo;
            _contentRepo = contentRepo;
            _wikiService = wikiService;
            _geminiService = geminiService;
        }

        // ── TOPIC ────────────────────────────────────────────────────

        public List<Topic> GetAllTopics() => _topicRepo.GetAllOrderedByDate();

        public Topic? GetTopicWithChapters(int id) => _topicRepo.GetByIdWithChapters(id);

        public async Task<Topic> GenerateTopicAsync(string topicName){
        
            // Bước 1: Lấy dữ liệu từ Wikipedia (Bao gồm cả Sections)
            var (exactTitle, summary, sections) = await _wikiService.GetTopicDataAsync(topicName);

            // Bước 2: Gọi Gemini sinh Curriculum
            List<dynamic> aiChapters = new();
            try
            {
                // Truyền cả summary và mảng sections vào Gemini
                aiChapters = await _geminiService.GenerateCurriculumAsync(summary, sections);
            }
            catch (Exception ex)
            {
                // Tạm thời ném thẳng lỗi ra ngoài để Controller bắt được và hiển thị lên UI
                throw new Exception($"Lấy Wiki thành công nhưng Gemini lỗi: {ex.Message}");
            }

            // Bước 3: Khởi tạo Topic (CHƯA LƯU VỘI - Chỉ tạo Object Graph trên RAM)
            var newTopic = new Topic
            {
                TopicName = exactTitle,
                Description = summary, // Chỉ lưu summary để DB gọn gàng hơn
                CreatedAt = DateTime.Now,
                Chapters = new List<Chapter>() // Khởi tạo danh sách chứa các Chương
            };

            // Bước 4: Xây dựng cấu trúc cây (Chapters -> Sections -> Lessons)
            int chapterOrder = 1;
            foreach (var item in aiChapters)
            {
                // Ép kiểu về JObject
                var aiChap = item as JObject;
                if (aiChap == null) continue;

                string chapterTitle = aiChap["ChapterTitle"]?.ToString();
                if (string.IsNullOrEmpty(chapterTitle)) continue;

                // Tạo Chapter (KHÔNG CẦN gán TopicId, KHÔNG gọi Save)
                var newChapter = new Chapter
                {
                    ChapterTitle = chapterTitle,
                    ChapterOrder = chapterOrder++,
                    CreatedAt = DateTime.Now,
                    Sections = new List<Section>() // Khởi tạo danh sách chứa các Phần
                };

                int lessonOrder = 1; // Khởi tạo thứ tự Lesson ở cấp Chapter để tăng dần liên tục

                // XỬ LÝ SECTIONS BÊN TRONG CHAPTER
                var sectionsArray = aiChap["Sections"] as JArray;
                if (sectionsArray != null)
                {
                    int sectionOrder = 1;
                    foreach (var secItem in sectionsArray)
                    {
                        var aiSec = secItem as JObject;
                        if (aiSec == null) continue;

                        string sectionTitle = aiSec["SectionTitle"]?.ToString();
                        if (string.IsNullOrEmpty(sectionTitle)) continue;

                        // Tạo Section (KHÔNG CẦN gán ChapterId, KHÔNG gọi Save)
                        var newSection = new Section
                        {
                            SectionTitle = sectionTitle,
                            SectionOrder = sectionOrder++,
                            Lessons = new List<Lesson>() // Khởi tạo danh sách chứa các Bài học
                        };

                        // XỬ LÝ LESSONS BÊN TRONG SECTION
                        var lessonsArray = aiSec["Lessons"] as JArray;
                        if (lessonsArray != null)
                        {
                            foreach (var lesson in lessonsArray)
                            {
                                // Tạo Lesson
                                newSection.Lessons.Add(new Lesson
                                {
                                    LessonTitle = lesson.ToString(),
                                    LessonOrder = lessonOrder++,
                                    // QUAN TRỌNG: Map thẳng vào object newChapter để Entity Framework
                                    // tự động hiểu và gắn ChapterId sau khi Save
                                    Chapter = newChapter 
                                });
                            }
                        }

                        // Gắn Section vào Chapter
                        newChapter.Sections.Add(newSection);
                    }
                }

                // Gắn Chapter vào Topic
                newTopic.Chapters.Add(newChapter);
            }

            // Bước 5: LƯU TẤT CẢ VÀO DATABASE TRONG 1 LẦN DUY NHẤT
            // Nhờ có Transaction, thao tác này diễn ra cực kỳ nhanh và an toàn (không bị rác dữ liệu nếu có lỗi giữa chừng)
            _topicRepo.Add(newTopic);
            await _topicRepo.SaveAsync();

            return newTopic;
        }

        public void DeleteTopic(int topicId)
        {
            var topic = _topicRepo.GetByIdWithChapters(topicId);
            if (topic != null)
            {
                _topicRepo.Delete(topic);
                _topicRepo.Save(); // EF Core sẽ tự động xóa các Chapter, Lesson con nếu bạn cấu hình Cascade Delete
            }
        }
        
        // ── CHAPTER ──────────────────────────────────────────────────

        public Chapter? GetChapterWithLessons(int id) => _chapterRepo.GetByIdWithLessons(id);

        public void CreateChapter(int topicId, string chapterTitle)
        {
            var topic = _topicRepo.GetByIdWithChapters(topicId);
            int nextOrder = (topic?.Chapters?.Count ?? 0) + 1;

            _chapterRepo.Add(new Chapter
            {
                TopicId = topicId,
                ChapterTitle = chapterTitle,
                ChapterOrder = nextOrder
            });
            _chapterRepo.Save();
        }

        public void DeleteChapter(int chapterId)
        {
            var chapter = _chapterRepo.GetByIdWithLessons(chapterId);
            if (chapter == null) return;
            _chapterRepo.Delete(chapter);
            _chapterRepo.Save();
        }

        // ── LESSON ───────────────────────────────────────────────────

        public void CreateLesson(int chapterId, string lessonTitle)
        {
            var chapter = _chapterRepo.GetByIdWithLessons(chapterId);
            int nextOrder = (chapter?.Lessons?.Count ?? 0) + 1;

            _lessonRepo.Add(new Lesson
            {
                ChapterId = chapterId,
                LessonTitle = lessonTitle,
                LessonOrder = nextOrder
            });
            _lessonRepo.Save();
        }

        public Lesson? GetLessonWithContext(int lessonId) => _lessonRepo.GetByIdWithContext(lessonId);

        // ── LESSON CONTENT ───────────────────────────────────────────

        public async Task<string> GenerateLessonContentAsync(int lessonId)
        {
            var lesson = _lessonRepo.GetByIdWithContext(lessonId)
                        ?? throw new Exception("Không tìm thấy bài học!");

            // Ép buộc kiểm tra tính toàn vẹn dữ liệu
            if (lesson.Chapter == null || lesson.Chapter.Topic == null)
            {
                throw new Exception("Dữ liệu bài học bị lỗi: Không tìm thấy Chương hoặc Chủ đề liên quan. Hãy kiểm tra lại hàm GetByIdWithContext đã có .Include() chưa.");
            }

            // Lúc này đã chắc chắn Chapter và Topic không null
            string topicName = lesson.Chapter.Topic.TopicName;
            string chapterTitle = lesson.Chapter.ChapterTitle ?? "Không rõ";
            int chapterOrder = lesson.Chapter.ChapterOrder ?? 1;
            string lessonTitle = lesson.LessonTitle ?? "Không rõ";
            int lessonOrder = lesson.LessonOrder ?? 1;

            // Truyền các biến ĐÃ ĐƯỢC XỬ LÝ AN TOÀN vào hàm gọi Gemini
            string htmlContent = await _geminiService.GenerateLessonContentAsync(
                topicName, 
                chapterOrder, 
                chapterTitle, 
                lessonOrder, 
                lessonTitle
            );

            _contentRepo.Add(new Content
            {
                LessonId = lessonId,
                ContentText = htmlContent,
                ContentOrder = _contentRepo.CountByLesson(lessonId) + 1,
                CreatedAt = DateTime.Now
            });
            
            await _contentRepo.SaveAsync();

            return htmlContent;
        }

        public List<Content> GetLessonContents(int lessonId) => _contentRepo.GetByLesson(lessonId);
    }
}