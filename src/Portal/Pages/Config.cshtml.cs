using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Portal.Model;
using Microsoft.AspNetCore.Mvc;

namespace Portal.Pages
{
    //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ConfigModel : PageModel
    {
        public static string AuthServerAuthority { get; set; }
        public static string ApplicationInsightsKey { get; set; }
        public static string GPTWWebSiteBaseUrl { get; set; }
        public static string GPTWStoreUrl { get; set; }
        public static string ZendeskAPIUrl { get; set; }
        public static string EmprisingUrl { get; set; }
        public static string IsAppMaintenanceMode { get; set; }
        public static string ExpectedEnvironmentClaim { get; set; }

        private AppSettings _appsettings;
        public ConfigModel(IOptions<AppSettings> options)
        {
            _appsettings = options.Value;
        }

        public void OnGet()
        {
            AuthServerAuthority = _appsettings.AuthServerAuthority;
            ApplicationInsightsKey = _appsettings.ApplicationInsightsKey;
            GPTWWebSiteBaseUrl = _appsettings.GPTWWebSiteBaseUrl;
            GPTWStoreUrl = _appsettings.GPTWStoreUrl;
            ZendeskAPIUrl = _appsettings.ZendeskAPIUrl;
            EmprisingUrl = _appsettings.EmprisingURL;
            IsAppMaintenanceMode = _appsettings.IsAppMaintenanceMode;
            ExpectedEnvironmentClaim = _appsettings.ExpectedEnvironmentClaim;
        }

        public ActionResult OnGetSettings()
        {
            var settings = $@"window.client_config = {{
    AuthServerAuthority: ""{_appsettings.AuthServerAuthority}"",
    ApplicationInsightsKey: ""{_appsettings.ApplicationInsightsKey}"",
    GPTWWebSiteBaseUrl: ""{_appsettings.GPTWWebSiteBaseUrl}"",
    GPTWStoreUrl: ""{_appsettings.GPTWStoreUrl}"",
    ZendeskAPIUrl: ""{_appsettings.ZendeskAPIUrl}"",
    EmprisingUrl: ""{_appsettings.EmprisingURL}"",
    IsAppMaintenanceMode: ""{_appsettings.IsAppMaintenanceMode}"",
    ExpectedEnvironmentClaim: ""{_appsettings.ExpectedEnvironmentClaim}""
}};";
            return Content(settings, "text/javascript; charset=utf-8");
        }
    }
}
