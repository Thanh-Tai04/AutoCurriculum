using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AutoCurriculum.Controllers
{
    [AllowAnonymous] // Mở cửa cho khách lạ vào xem
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Nếu chưa đăng nhập, cho xem quảng cáo (Landing Page)
            return View();
        }
    }
}