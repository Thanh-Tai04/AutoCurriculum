using Microsoft.AspNetCore.Identity;

namespace AutoCurriculum.Models
{
    // Kế thừa IdentityUser để lấy sẵn các cột ID, Email, PasswordHash...
    public class ApplicationUser : IdentityUser
    {
        // Thêm các cột tùy chỉnh cho Đồ án của bạn
        public string? FullName { get; set; } 
        // public string StudentId { get; set; } // Nếu thích có thể mở comment dòng này để lưu MSSV
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}