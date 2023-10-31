using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Net;
using SharedProject2;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Model;
using Portal.Controllers;
using System.Runtime.InteropServices;

namespace Portal.Controllers
{
    [Route("api")]
    [ApiController]
    public class EmailTrackingController : PortalControllerBase
    {
        public EmailTrackingController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        const string RelativePathToGPTW_Logo = "..\\Portal\\ClientApp\\src\\assets\\images\\logo.png";

        // api/ImageAutomation?type=logo&id=620d4bdc064eae4bb4e105e3&resourceName=logo.png

        [HttpGet("[action]")]
        public IActionResult ImageAutomation(string type, string id, string resourceName)
        {
            try
            {
                if (id != "0") // Special id that is skipped for the SendToolKit email because that email is the person who is sent the badge 
                {
                    EmailTracking emailTracking = this.AORepository.GetEmailTracking(id);
                    if (emailTracking == null)
                        AtlasLog.LogError(String.Format("ImageAutomation error. EmailTracking record is missing for id: {0}", id));
                    else
                    {
                        emailTracking.Opened = true;
                        AtlasLog.LogInformation(String.Format("ImageAutomation called for cid:{0}, eid:{1}, email:{2}, id:{3}", emailTracking.ClientId, emailTracking.EngagementId, emailTracking.Address, emailTracking.Id.ToString()));
                        emailTracking.DateTimeOpened.Add(DateTime.UtcNow);
                        this.AORepository.SaveEmailTracking(emailTracking);
                    }
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("ImageAutomation EmailTracking Exception caught."), e);
            }

            // No need to dispose the stream, MVC does it for you
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClientApp\\src\\assets\\images\\", "gptw_logo.png");
            FileStream stream = new FileStream(path, FileMode.Open);
            FileStreamResult result = new FileStreamResult(stream, "image/png");
            result.FileDownloadName = "image.png";
            return result;
        }

    }
}


