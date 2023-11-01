using GPTW.ListAutomation.Core.Services.Data;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Portal.Model;

namespace Affiliates.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListRequestController : ControllerBase //PortalControllerBase
    {

        private readonly ICountryService _countryService;
        private readonly IListRequestService _listRequestService;
        private readonly ISegmentService _segmentService;

        public ListRequestController(
            ICountryService countryService,
            IListRequestService listRequestService,
            ISegmentService segmentService)
        {
            _countryService = countryService;
            _listRequestService = listRequestService;
            _segmentService = segmentService;
        }

        public IActionResult Filter([DataSourceRequest] DataSourceRequest request)
        {
            var data = _listRequestService.GetTable().Select(c => new ListRequestItemModel()
            {
                ListRequestId = c.ListRequestId,
                CountryCode = c.CountryCode,
                PublicationYear = c.PublicationYear,
                ModifiedDateTime = c.ModifiedDateTime,
                ListName = c.ListName,
                ListNameLocalLanguage = c.ListNameLocalLanguage,
                LicenseId = c.LicenseId,
                NumberOfWinners = c.NumberOfWinners,
                SegmentName = c.Segment.SegmentName,
                UploadStatus = c.UploadStatus.StatusName
            });

            return new JsonResult(data.ToDataSourceResult(request));
        }

    }
}
