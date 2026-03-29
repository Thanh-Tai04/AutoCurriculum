using System.ComponentModel.DataAnnotations;

namespace AutoCurriculum.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty; // <--- Thêm vào đây

        [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
        public string Token { get; set; } = string.Empty; // <--- Thêm vào đây

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$", 
            ErrorMessage = "Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt (VD: @, $, !).")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty; // <--- Thêm vào đây
    }
}