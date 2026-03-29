using Microsoft.AspNetCore.Mvc;
using AutoCurriculum.Services.Interfaces;
using Rotativa.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutoCurriculum.Controllers
{
    [Authorize]
    public class ExportController : Controller
    {
        private readonly ICurriculumService _curriculumService;

        public ExportController(ICurriculumService curriculumService)
        {
            _curriculumService = curriculumService;
        }

        [HttpGet]
        public IActionResult Preview(int id)
        {
            var topic = _curriculumService.GetTopicWithChapters(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Chặn người lạ tải PDF của mình
            if (topic == null || topic.UserId != userId) return Forbid();

            // Trả về thư mục Views/Export/ExportToPdf.cshtml (bạn cần di chuyển file HTML sang thư mục này)
            return View("ExportToPdf", topic);
        }

        [HttpGet]
        public IActionResult ExportToPdf(int id)
        {
            var topic = _curriculumService.GetTopicWithChapters(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (topic == null || topic.UserId != userId) return Forbid();

            string cleanFileName = AutoCurriculum.Helpers.StringHelper.ConvertToSlug(topic.TopicName);

            return new ViewAsPdf("ExportToPdf", topic)
            {
                FileName = $"GiaoTrinh_{cleanFileName}.pdf", 
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(20, 20, 20, 30),
                CustomSwitches = "--disable-smart-shrinking --print-media-type --footer-center \"[page]\" --footer-font-size \"13\" --footer-font-name \"Times New Roman\""
            };
        }
    }
}