using GPTW.ListAutomation.Core.Services.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Affiliates.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SegmentController : ControllerBase //PortalControllerBase
    {
        private readonly ISegmentService _segmentService;

        public SegmentController(ISegmentService segmentService)
        {
            _segmentService = segmentService;
        }

        public JsonResult GetAllSegments()
        {
            var data = _segmentService.GetTable().ToList();

            return new JsonResult(data);
        }
    }
}
