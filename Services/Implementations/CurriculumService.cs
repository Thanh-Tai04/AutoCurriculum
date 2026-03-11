using AutoCurriculum.Models;
using AutoCurriculum.Repositories.Interfaces;
using AutoCurriculum.Services.Interfaces;

namespace AutoCurriculum.Services.Implementations
{
    public class CurriculumService : ICurriculumService
    {
        private readonly ITopicRepository _topicRepo;
        private readonly IChapterRepository _chapterRepo;
        private readonly ILessonRepository _lessonRepo;
        private readonly IContentRepository _contentRepo;
        private readonly IWikipediaService _wikiService;
        private readonly IGeminiService _geminiService;

        public CurriculumService(
            ITopicRepository topicRepo,
            IChapterRepository chapterRepo,
            ILessonRepository lessonRepo,
            IContentRepository contentRepo,
            IWikipediaService wikiService,
            IGeminiService geminiService)
        {
            _topicRepo = topicRepo;
            _chapterRepo = chapterRepo;
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
            // Bước 1: Lấy dữ liệu từ Wikipedia
            var (exactTitle, summary, sections) = await _wikiService.GetTopicDataAsync(topicName);

            string wikiDescription = summary;
            if (sections.Count > 0)
                wikiDescription += "\n\n📌 [KHUNG CHƯƠNG TỪ WIKIPEDIA]: \n- " + string.Join("\n- ", sections);

            // Bước 2: Gọi Gemini sinh Curriculum
            List<dynamic> aiChapters = new();
            try
            {
                aiChapters = await _geminiService.GenerateCurriculumAsync(wikiDescription);
            }
            catch (Exception ex)
            {
                // Tạm thời ném thẳng lỗi ra ngoài để Controller bắt được và hiển thị lên UI
                throw new Exception($"Lấy Wiki thành công nhưng Gemini lỗi: {ex.Message}");
            }

            // Bước 3: Lưu Topic
            var newTopic = new Topic
            {
                TopicName = exactTitle,
                Description = wikiDescription,
                CreatedAt = DateTime.Now
            };
            _topicRepo.Add(newTopic);
            await _topicRepo.SaveAsync();

            // Bước 4: Tạo Chapters + Lessons từ kết quả AI
            int chapterOrder = 1;
            foreach (var item in aiChapters)
            {
                // Ép kiểu rõ ràng từ dynamic sang JObject để C# không bị "lú"
                var aiChap = item as Newtonsoft.Json.Linq.JObject;
                if (aiChap == null) continue;

                string chapterTitle = aiChap["ChapterTitle"]?.ToString();
                if (string.IsNullOrEmpty(chapterTitle)) continue;

                var newChapter = new Chapter
                {
                    TopicId = newTopic.TopicId,
                    ChapterTitle = chapterTitle,
                    ChapterOrder = chapterOrder++
                };
                _chapterRepo.Add(newChapter);
                await _chapterRepo.SaveAsync(); // Bắt buộc Save để Entity Framework sinh ra ChapterId

                // Lấy mảng bài học và ép kiểu thẳng về JArray
                var lessonsArray = aiChap["Lessons"] as Newtonsoft.Json.Linq.JArray;
                if (lessonsArray != null)
                {
                    int lessonOrder = 1;
                    foreach (var lesson in lessonsArray)
                    {
                        _lessonRepo.Add(new Lesson
                        {
                            ChapterId = newChapter.ChapterId,
                            LessonTitle = lesson.ToString(),
                            LessonOrder = lessonOrder++
                        });
                    }
                }
            }

            // Lưu toàn bộ Lessons vào Database trong 1 lần gọi
            await _lessonRepo.SaveAsync();

            return newTopic;
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

            string topicName = lesson.Chapter?.Topic?.TopicName ?? "Không rõ";
            string chapterTitle = lesson.Chapter?.ChapterTitle ?? "Không rõ";
            string lessonTitle = lesson.LessonTitle;

            string htmlContent = await _geminiService.GenerateLessonContentAsync(topicName, chapterTitle, lessonTitle);

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