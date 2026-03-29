using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Models; 
using System.Linq;

namespace AutoCurriculum.Areas.Admin.Controllers
{
    [Area("Admin")] 
    public class DashboardController : Controller
    {
        private readonly AutoCurriculumDbContext _context;

        public DashboardController(AutoCurriculumDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var logs = _context.SystemLogs
                               .OrderByDescending(x => x.CreatedAt)
                               .Take(20)
                               .ToList();

            return View(logs);
        }
    }
}