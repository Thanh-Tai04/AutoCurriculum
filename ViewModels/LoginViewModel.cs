using System.ComponentModel.DataAnnotations;

namespace AutoCurriculum.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập hoặc Email.")]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public string UsernameOrEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}