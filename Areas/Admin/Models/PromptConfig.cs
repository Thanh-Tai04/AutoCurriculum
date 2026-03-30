using System;
using System.ComponentModel.DataAnnotations;

namespace AutoCurriculum.Models
{
    public class PromptConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string PromptCode { get; set; } // Dùng để code C# gọi đúng (VD: "Generate_Curriculum")

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // Tên hiển thị trên web (VD: "Câu lệnh tạo Cấu trúc Giáo trình")

        public string Description { get; set; } // Ghi chú để nhớ các biến như {topicName}, {lessonTitle}...

        [Required]
        public string PromptText { get; set; } // Nội dung câu lệnh siêu dài của bạn sẽ nằm ở đây

        public DateTime UpdatedAt { get; set; } = DateTime.Now; // Lưu thời gian chỉnh sửa cuối cùng
    }
}