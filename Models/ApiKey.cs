using System;
using System.ComponentModel.DataAnnotations;

namespace AutoCurriculum.Models
{
    public class ApiKey
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ (Provider)")]
        [StringLength(100)]
        public string Provider { get; set; } // Ví dụ: "Gemini AI", "Wikipedia"

        [Required(ErrorMessage = "Vui lòng nhập mã API Key")]
        public string KeyValue { get; set; } // Chuỗi khóa thực tế

        public bool IsActive { get; set; } = true; // Trạng thái Bật/Tắt

        public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày thêm khóa
    }
}