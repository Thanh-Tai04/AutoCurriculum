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

            if (topic == null || topic.UserId != userId) return Forbid();

            string cleanFileName = AutoCurriculum.Helpers.StringHelper.ConvertToSlug(topic.TopicName);

            return new ViewAsPdf("ExportToPdf", topic)
            {
                
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(20, 20, 20, 30),
                CustomSwitches = "--disable-smart-shrinking --print-media-type --footer-center \"[page]\" --footer-font-size \"13\" --footer-font-name \"Times New Roman\""
            };
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
                PageMargins = new Rotativa.AspNetCore.Options.Margins(25, 20, 25, 35),
                CustomSwitches = "--disable-smart-shrinking --print-media-type --footer-center \"[page]\" --footer-font-size \"13\" --footer-font-name \"Times New Roman\""
            };
        }
    }
}