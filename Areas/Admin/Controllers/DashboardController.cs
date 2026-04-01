using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Models; 
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace AutoCurriculum.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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
                               .ToList();

            return View(logs);
        }
    }
}