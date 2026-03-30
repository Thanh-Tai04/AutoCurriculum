using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Models;
using System.Linq;
using System;

namespace AutoCurriculum.Areas.Admin.Controllers
{
    [Area("Admin")]
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
                        PromptText = "Bạn là một Giáo sư Đại học đa ngành. Dựa vào nội dung nền tảng và CẤU TRÚC MỤC LỤC từ Wikipedia sau:\n[CHỦ ĐỀ]: {topicName}\n[TÓM TẮT NỘI DUNG NỀN TẢNG]: {wikiDescription}\n[DANH SÁCH MỤC LỤC THAM KHẢO]: \n- {sectionsText}\n\nYÊU CẦU CHI TIẾT:\n1. Dựa vào mục lục tham khảo, hãy phân chia thành các Chương (Chapters) logic.\n2. TRONG MỖI CHƯƠNG, bạn phải tự suy luận và chia nhỏ thành ít nhất 3-5 Bài học (Lessons) cụ thể để đảm bảo truyền tải hết kiến thức.\n3. Tên bài học phải mang tính chuyên môn cao, không được đặt tên chung chung.\n4. CHỈ TRẢ VỀ ĐỊNH DẠNG JSON THUẦN TÚY theo đúng cấu trúc hệ thống yêu cầu. Tuyệt đối không trả về Markdown (```json), không có lời dẫn. Chỉ JSON." 
                    },
                    new PromptConfig 
                    { 
                        PromptCode = "Generate_Lesson", 
                        Name = "2. Lệnh tạo Nội dung Bài học", 
                        Description = "Các biến bắt buộc phải giữ nguyên trong bài: {topicName}, {chapterOrder}, {chapterTitle}, {lessonNumber}, {lessonTitle}", 
                        PromptText = "Bạn là một Giảng viên Đại học biên soạn tài liệu giáo trình. Hãy biên soạn nội dung giảng dạy CHI TIẾT cho bài học sau:\n- Nằm trong môn học/chủ đề: {topicName}\n- Thuộc Chương {chapterOrder}: {chapterTitle}\n- Tên bài học hiện tại: Bài {lessonNumber} - {lessonTitle}\n\nYÊU CẦU BẮT BUỘC:\n1. TRÌNH BÀY MỤC LỤC CHUẨN: Sử dụng mã số bài học ({lessonNumber}) làm gốc để đánh số phân cấp cho các nội dung bên trong.\n- Các mục chính (dùng thẻ <h3>) BẮT BUỘC đánh số: {lessonNumber}.1, {lessonNumber}.2...\n- Các tiểu mục con (dùng thẻ <h4>) BẮT BUỘC đánh số: {lessonNumber}.1.1, {lessonNumber}.1.2...\n2. NỘI DUNG: Viết một bài giảng sâu sắc, dễ hiểu, văn phong học thuật.\n3. THỰC TẾ: Bắt buộc có ví dụ minh họa thực tế. Nếu là CNTT, bắt buộc có đoạn code mẫu.\n4. ĐỊNH DẠNG: Trình bày bằng HTML cơ bản (dùng thẻ <h3>, <h4>, <p>, <ul>, <li>, <strong>, <code>). KHÔNG dùng markdown.\n5. KHÔNG trả về JSON. Chỉ trả về trực tiếp đoạn mã HTML nội dung bài học.\n6. TUYỆT ĐỐI KHÔNG CHÀO HỎI.\n7. VÀO THẲNG VẤN ĐỀ: Đoạn HTML trả về BẮT BUỘC phải bắt đầu ngay lập tức bằng thẻ <h3>." 
                    }
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