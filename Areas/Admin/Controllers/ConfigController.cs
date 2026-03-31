using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Models;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authorization;

namespace AutoCurriculum.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ConfigController : Controller
    {
        private readonly AutoCurriculumDbContext _context;

        public ConfigController(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // TỰ ĐỘNG THÊM DỮ LIỆU MẪU (SEEDING) NẾU BẢNG TRỐNG
            if (!_context.PromptConfigs.Any())
            {
                _context.PromptConfigs.AddRange(
                    new PromptConfig 
                    { 
                        PromptCode = "Generate_Curriculum", 
                        Name = "1. Lệnh tạo Cấu trúc Giáo trình", 
                        Description = "Các biến bắt buộc phải giữ nguyên trong bài: {topicName}, {sourceUrl}, {wikiDescription}, {sectionsText}", 
                        PromptText = "Bạn là một Giáo sư Đại học đa ngành. Dựa vào nội dung nền tảng và CẤU TRÚC MỤC LỤC từ Wikipedia sau:\n\n[CHỦ ĐỀ]: {topicName}\n[SOURCE URL]: {sourceUrl}\n[TÓM TẮT NỘI DUNG]:\n{wikiDescription}\n\n[MỤC LỤC GỐC TỪ WIKIPEDIA]:\n- {sectionsText}\n\nYÊU CẦU BẮT BUỘC:\n1. ĐÓNG VAI CHUYÊN GIA: Tự động phân tích xem chủ đề trên thuộc lĩnh vực nào (Kinh tế, CNTT, Y học, Lịch sử...).\n2. NÂNG CẤP HỌC THUẬT: Biến nội dung này thành một GIÁO TRÌNH BÀI BẢN. Sử dụng [MỤC LỤC GỐC TỪ WIKIPEDIA] làm khung sườn chính để chia Chương (Chapter) và Mục con (Section). Tự bổ sung kiến thức chuyên sâu đặc thù của ngành đó.\n3. CẤU TRÚC: Thiết kế TỐI ĐA 5 chương. Mỗi chương bao gồm các Mục con (Section). Mỗi Mục con chứa 2-4 bài học (Lesson) đi từ cơ bản đến nâng cao.\n4. QUY TẮC ĐẶT TÊN (RẤT QUAN TRỌNG): TUYỆT ĐỐI KHÔNG thêm các tiền tố như 'Chương 1:', 'Bài 1:', 'Phần 1.1:', '1.', 'a.' vào đầu tên của ChapterTitle, SectionTitle hay Lessons. Chỉ sinh ra phần nội dung tiêu đề thuần túy.\n5. ĐẦU RA: CHỈ trả về MỘT OBJECT JSON DUY NHẤT, KHÔNG dùng thẻ markdown ```json, không giải thích thêm. BẮT BUỘC phải theo đúng cấu trúc dưới đây:\n{{\n    \"\"TopicName\"\": \"\"{topicName}\"\",\n    \"\"Description\"\": \"\"Tóm tắt mục tiêu của giáo trình này (khoảng 2-3 câu).\"\",\n    \"\"SourceName\"\": \"\"{topicName}\"\",\n    \"\"SourceUrl\"\": \"\"{sourceUrl}\"\",\n    \"\"Chapters\"\": [\n    {{\n        \"\"ChapterTitle\"\": \"\"Tổng quan và Lịch sử phát triển\"\",\n        \"\"Sections\"\": [\n        {{\n            \"\"SectionTitle\"\": \"\"Khái niệm cốt lõi\"\",\n            \"\"Lessons\"\": [\"\"Định nghĩa cơ bản\"\", \"\"Ứng dụng và tầm quan trọng\"\"]\n        }}\n        ]\n    }}\n    ]\n}}"                    },
                    new PromptConfig 
                    { 
                        PromptCode = "Generate_Lesson", 
                        Name = "2. Lệnh tạo Nội dung Bài học", 
                        Description = "Các biến bắt buộc phải giữ nguyên trong bài: {topicName}, {chapterOrder}, {chapterTitle}, {lessonNumber}, {lessonTitle}", 
                        PromptText = "Bạn là một Giảng viên Đại học biên soạn tài liệu giáo trình. Hãy biên soạn nội dung giảng dạy CHI TIẾT cho bài học sau:\n" +
                                        "\n- Nằm trong môn học/chủ đề: {topicName}" +
                                        "\n- Thuộc Chương {chapterOrder}: {chapterTitle}" +
                                        "\n- Tên bài học hiện tại: Bài {lessonNumber} - {lessonTitle}\n" +

                                        "\nYÊU CẦU BẮT BUỘC:" +
                                        "\n1. TRÌNH BÀY MỤC LỤC CHUẨN:" +
                                        "\n- Sử dụng mã số bài học ({lessonNumber}) làm gốc để đánh số." +
                                        "\n- Các mục chính dùng thẻ <h3>: {lessonNumber}.1, {lessonNumber}.2, ..." +
                                        "\n- Các tiểu mục dùng thẻ <h4>: {lessonNumber}.1.1, {lessonNumber}.1.2, ..." +

                                        "\n2. NỘI DUNG:" +
                                        "\n- Viết bài giảng có chiều sâu, dễ hiểu, văn phong học thuật." +
                                        "\n- Giải thích rõ khái niệm, nguyên lý, và cách áp dụng." +

                                        "\n3. THỰC TẾ:" +
                                        "\n- BẮT BUỘC có ví dụ minh họa thực tế." +
                                        "\n- Nếu chủ đề thuộc CNTT: PHẢI có ít nhất 1 đoạn code mẫu đặt trong thẻ <code>." +

                                        "\n4. ĐỊNH DẠNG HTML:" +
                                        "\n- Chỉ sử dụng các thẻ: <h3>, <h4>, <p>, <ul>, <li>, <strong>, <code>." +
                                        "\n- KHÔNG sử dụng Markdown." +
                                        "\n- Không dùng thẻ ngoài danh sách." +

                                        "\n5. ĐẦU RA:" +
                                        "\n- KHÔNG trả về JSON." +
                                        "\n- Chỉ trả về DUY NHẤT đoạn HTML." +

                                        "\n6. TUYỆT ĐỐI KHÔNG CHÀO HỎI:" +
                                        "\n- Không được dùng các câu như: 'Chào...', 'Hôm nay...', 'Chúng ta sẽ...'" +

                                        "\n7. BẮT BUỘC BẮT ĐẦU ĐÚNG:" +
                                        "\n- Dòng đầu tiên PHẢI là thẻ <h3> của mục {lessonNumber}.1." +
                                        "\n- KHÔNG có bất kỳ nội dung nào trước thẻ <h3> này." +

                                        "\n8. RÀNG BUỘC QUAN TRỌNG:" +
                                        "\n- Không giải thích thêm ngoài HTML." +
                                        "\n- Không thêm tiêu đề ngoài hệ thống đánh số." +
                                        "\n- Đảm bảo thứ tự logic từ cơ bản đến nâng cao."                    }
                );
                _context.SaveChanges();
            }

            var prompts = _context.PromptConfigs.ToList();
            return View(prompts);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var prompt = _context.PromptConfigs.Find(id);
            if (prompt == null) return NotFound();
            return View(prompt);
        }

        [HttpPost]
        public IActionResult Edit(PromptConfig model)
        {
            // BƯỚC CHẶN 1: Kiểm tra nếu rỗng hoặc toàn dấu cách
            if (string.IsNullOrWhiteSpace(model.PromptText))
            {
                TempData["ErrorMessage"] = "Lỗi: Nội dung Prompt không được để trống!";
                return RedirectToAction("Edit", new { id = model.Id }); // Đẩy về lại trang sửa
            }

            var existing = _context.PromptConfigs.Find(model.Id);
            if (existing != null)
            {
                // BƯỚC CHẶN 2: Kiểm tra xem có lỡ tay xóa mất biến ngoặc nhọn không
                if (!model.PromptText.Contains("{topicName}"))
                {
                    TempData["ErrorMessage"] = "Lỗi: Bạn đã xóa mất biến bắt buộc {topicName}. Vui lòng thêm lại!";
                    return RedirectToAction("Edit", new { id = model.Id });
                }

                existing.PromptText = model.PromptText;
                existing.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã cập nhật câu lệnh thành công!";
            }
            return RedirectToAction("Index");
        }
    }
}