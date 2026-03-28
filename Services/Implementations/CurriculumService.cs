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

        public async Task<Topic> GenerateTopicAsync(string topicName)
        {
            // Bước 1: Lấy dữ liệu từ Wikipedia (Bao gồm cả Sections)
            var (exactTitle, summary, sections) = await _wikiService.GetTopicDataAsync(topicName);

            // Tự động tạo Link gốc của Wikipedia để lát nữa làm Nguồn tham khảo
            string wikiUrl = $"https://vi.wikipedia.org/wiki/{Uri.EscapeDataString(exactTitle.Replace(" ", "_"))}";

            // Bước 2: Gọi Gemini sinh Curriculum (Trả về thẳng Object DTO, không cần JObject nữa)
            AutoCurriculum.ViewModels.AiCurriculumDto aiData;
            try
            {
                // Truyền đủ 4 tham số mới cập nhật vào Gemini
                aiData = await _geminiService.GenerateCurriculumAsync(exactTitle, wikiUrl, summary, sections);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lấy Wiki thành công nhưng Gemini lỗi: {ex.Message}");
            }

            // Bước 3: Khởi tạo Nguồn (Source) và Topic (CHƯA LƯU VỘI)
            var newSource = new Source
            {
                SourceName = aiData.SourceName ?? exactTitle,
                SourceUrl = aiData.SourceUrl ?? wikiUrl,
                RetrievedDate = DateTime.Now
            };

            var newTopic = new Topic
            {
                TopicName = aiData.TopicName ?? exactTitle,
                Description = summary, // Chỉ lưu summary để DB gọn gàng hơn
                Source = newSource,    // GẮN NGUỒN VÀO ĐÂY: Entity Framework sẽ tự động lưu Source trước để lấy ID
                CreatedAt = DateTime.Now,
                Chapters = new List<Chapter>() 
            };

            // Bước 4: Xây dựng cấu trúc cây (Mọi thứ giờ đây đã được strongly-typed nhờ DTO)
            int chapterOrder = 1;
            foreach (var chapDto in aiData.Chapters)
            {
                var newChapter = new Chapter
                {
                    ChapterTitle = chapDto.ChapterTitle,
                    ChapterOrder = chapterOrder++,
                    CreatedAt = DateTime.Now,
                    Sections = new List<Section>()
                };

                int sectionOrder = 1;
                int lessonOrder = 1; // Giữ thứ tự Lesson tăng dần xuyên suốt trong 1 Chapter

                foreach (var secDto in chapDto.Sections)
                {
                    var newSection = new Section
                    {
                        SectionTitle = secDto.SectionTitle,
                        SectionOrder = sectionOrder++,
                        Lessons = new List<Lesson>()
                    };

                    foreach (var lessonTitle in secDto.Lessons)
                    {
                        newSection.Lessons.Add(new Lesson
                        {
                            LessonTitle = lessonTitle,
                            LessonOrder = lessonOrder++,
                            Chapter = newChapter // Map thẳng vào Chapter giống như logic trước đây của bạn
                        });
                    }

                    newChapter.Sections.Add(newSection);
                }

                newTopic.Chapters.Add(newChapter);
            }

            // Bước 5: LƯU TẤT CẢ VÀO DATABASE TRONG 1 LẦN DUY NHẤT
            // Phép màu của Entity Framework: Nó sẽ tự động INSERT bảng Source -> lấy SourceId gắn cho Topic 
            // -> INSERT Topic -> lấy TopicId gắn cho Chapter... và cứ thế đến Lesson.
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