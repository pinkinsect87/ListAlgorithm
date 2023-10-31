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
using System.Web.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using SharedProject2;
using Portal.Model;
using System.Web;
using Portal.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights;


namespace Reports.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientReportDownloadController : PortalControllerBase
    {

        public ClientReportDownloadController(TelemetryClient appInsights, IOptions<AppSettings> appSettings) : base(appInsights, appSettings)
        {
        }


        [HttpGet]
        public HttpResponseMessage DownloadClientReport(int reportFileId, string reportFileUri,  int clientId)
        {
            HttpResponseMessage resp = new HttpResponseMessage { StatusCode = HttpStatusCode.OK };

            // check other parameters
            ClientCenterRepository repo = new ClientCenterRepository(appSettings.ClientCenterMongoConnectionString);
            var rb = repo.RetrieveClientReportBundle(clientId);

            // ensure the report bundle looks ok
            // otherwise return the error file
            if (rb == null)
            {
                SetupFileErrorNotPossible(ref resp);
                return resp;
            }
            else if (rb.ReportList.Count == 0)
            {
                SetupFileErrorNotPossible(ref resp);
                return resp;
            }

            // ensure the file id can be found in the report bundle and the parameters match
            CCClientReport file = (from a in rb.ReportList
                        where a.FileUri.ToLower() == reportFileUri.ToLower() && a.ReportFileId == reportFileId
                        select a).FirstOrDefault();


            // if a match cannot be found, return the error file
            if (file == null)
            {
                SetupFileErrorNotPossible(ref resp);
                return resp;
            }

            // everything looks ok, attempt to retrieve the file from report store
            // and stream to user
            try
            {
                // retrieve the file
                AtlasReportStoreFileStreamer fileStreamer = new AtlasReportStoreFileStreamer(appSettings.ReportStoreUrl, file.AffiliateId, "api@greatplacetowork.com", appSettings.ReportStorePassword);
                var reportFile = fileStreamer.GetStream(new GptwUri() { Uri = file.FileUri });

                // set up the headers
                resp.Content = new StreamContent(reportFile);
                resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                resp.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                resp.Content.Headers.ContentDisposition.FileName = file.GetReportFriendlyName(true);
                    
                // return the result
                return resp;
            }
            catch (Exception ex)
            {
                SetupFileErrorNotPossible(ref resp);
                return resp;
            }
        }

        [NonAction]
        public void SetupFileErrorNotPossible(ref HttpResponseMessage resp)
        {
            resp.Content = new StringContent("Sorry, you cannot download the file. If you continue to receive this message, please contact our support team.");
            resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            resp.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            resp.Content.Headers.ContentDisposition.FileName = "error_not_possible.txt";
        }
    }
}
