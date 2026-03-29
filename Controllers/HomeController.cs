using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AutoCurriculum.Controllers
{
    [AllowAnonymous] // Mở cửa cho khách lạ vào xem
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Nếu khách đã đăng nhập (có thẻ bài), đá thẳng vào trang Giáo trình
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Curriculum");
            }

            // Nếu chưa đăng nhập, cho xem quảng cáo (Landing Page)
            return View();
        }
    }
}