using System.ComponentModel.DataAnnotations;

namespace AutoCurriculum.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập.")]
        [StringLength(50, ErrorMessage = "{0} phải từ {2} đến {1} ký tự.", MinimumLength = 3)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$", 
            ErrorMessage = "Mật khẩu từ 6 kí tự phải có chữ hoa, chữ thường, số và ký tự đặc biệt (VD: @, $, !).")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }
}