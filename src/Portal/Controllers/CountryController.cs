using GPTW.ListAutomation.Core.Services.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Affiliates.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase //PortalControllerBase
    {
        private readonly ICountryService _countryService;

        public CountryController(ICountryService countryService)
        {
            _countryService = countryService;
        }

        public JsonResult GetAllCountries()
        {
            var data = _countryService.GetTable()
                .Select(c => new { c.CountryCode, c.CountryName })
                .OrderBy(c => c.CountryName)
                .ToList();

            return new JsonResult(data);
        }
    }
}
