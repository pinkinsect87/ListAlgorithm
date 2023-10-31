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
//using System.Web.Http;
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


namespace Affiliates.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListRequestController : ControllerBase //PortalControllerBase
    {

        public ListRequestController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {

        }

    }
}
