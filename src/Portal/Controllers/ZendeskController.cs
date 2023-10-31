using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Portal.Model;
using Portal.Controllers;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using System.Configuration;
using System.Web;
using ZendeskApi_v2.Models.Tickets;
using System.Text;
using System.Net;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using System.Threading;

namespace Zendesk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZendeskController : PortalControllerBase
    {
        public ZendeskController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        [HttpGet("[action]")]
        public CreateHelpDeskTicketResult CreateHelpDeskTicket(string email, string comment, string companyname, string name, string phone)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("CreateHelpDeskTicket");
            CreateHelpDeskTicketResult result = new CreateHelpDeskTicketResult  {IsError = true };

            try
            {
                ZendeskApi_v2.ZendeskApi api = new ZendeskApi_v2.ZendeskApi(appSettings.ZendeskAPIUrl, appSettings.ZendeskUsername, appSettings.ZendeskPassword);

                StringBuilder allinfo = new StringBuilder();
                allinfo.AppendFormat("Name: {0}", name).AppendLine();
                allinfo.AppendFormat("Phone: {0}", phone).AppendLine();
                allinfo.AppendFormat("Email: {0}", email).AppendLine();
                allinfo.AppendFormat("Company: {0}", companyname).AppendLine();
                allinfo.AppendFormat("Comment: {0}", comment).AppendLine();

                IndividualTicketResponse response = api.Tickets.CreateTicket
                (
                    new ZendeskApi_v2.Models.Tickets.Ticket
                    {

                        Status = "new",
                        RequesterId = appSettings.ZendeskRequesterId,
                        GroupId = appSettings.ZendeskGroupId,
                        Subject = "TI Integrity " + companyname,
                        Type = "Incident",
                        Comment = new ZendeskApi_v2.Models.Tickets.Comment
                        {
                            Body = allinfo.ToString(),
                            Public = false,
                        }
                    }
                 );

                if (response == null || response.Ticket == null)
                    result.IsError = true;
                else
                    result.IsError = false;

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.IsError = true;
            }

            Thread.Sleep(5000);

            return result;
        }

        [HttpGet("[action]")]
        public GetHelpDeskLoginURLResult GetHelpDeskLoginURL(string returnTo)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetHelpDeskLoginURL");

            GetHelpDeskLoginURLResult result = new GetHelpDeskLoginURLResult
            {
                ErrorOccurred = true,
                ErrorMessage = "A general error has occurred.",
                URL = ""
            };

            try
            {
                // There is no authorization check here because this controller only requires 
                // authentication and this code won't run without having a token.

                string emailClaimName = "email";
                string email = "";
                string name = "";
                string clientId = "";
                string role = "end-user";
                ClaimResult cr;

                cr = GetClaim(gptwContext, "name");
                name = cr.value;
                bool foundName = cr.isSuccess;
                bool foundClientId = false;

                if (IsGPTWEmployee(gptwContext))
                {
                    emailClaimName = "upn";
                    foundClientId = true;
                    clientId = this.appSettings.GPTWClientId.ToString();
                    string zendeskAdminClaim = "";
                    cr = GetClaim(gptwContext, "GptwAd_GptwAppRole_ZendeskAdmin");
                    bool foundAdminClaim = cr.isSuccess;
                    zendeskAdminClaim = cr.value;

                    if (foundAdminClaim && zendeskAdminClaim == "GptwAppRole_ZendeskAdmin")
                    {
                        role = "admin";
                    }
                    else
                    {
                        // Agent claim is secondary to Admin claim
                        cr = GetClaim(gptwContext, "GptwAd_GptwAppRole_ZendeskAgent");
                        bool foundAgentClaim = cr.isSuccess;
                        string zendeskAgentClaim = cr.value;

                        if (foundAgentClaim && zendeskAgentClaim == "GptwAppRole_ZendeskAgent")
                        {
                            role = "agent";
                        }
                    }
                }
                else
                {
                   emailClaimName = "email";
                   cr = GetClaim(gptwContext, "gptw_client_id");
                   foundClientId = cr.isSuccess;
                   clientId = cr.value;
                }

                cr = GetClaim(gptwContext, emailClaimName);
                bool foundEmail = cr.isSuccess;
                email = cr.value;

                if (!foundEmail || !foundName || !foundClientId)
                {
                    AtlasLog.LogError(String.Format("ERROR. email:{0},clientId:{1},foundEmail:{2},foundName:{3},foundClientId:{4},role:{5}", email, clientId, foundEmail, foundName, foundClientId, role), gptwContext);
                    result.ErrorMessage = "Couldn't retrieve email/name/clientId which is neccessary.";
                    result.ErrorOccurred = true;
                    return result;
                }

                AtlasLog.LogInformation(String.Format("BuildZendesk JWT Token. email:{0},clientId:{1},foundEmail:{2},foundName:{3},foundClientId:{4},role:{5}", email, clientId, foundEmail, foundName, foundClientId, role), gptwContext);

                string jwt = BuildZenDeskJWT(clientId, email, name, role, gptwContext);

                if (!String.IsNullOrEmpty(jwt))
                {
                    result.URL = this.appSettings.ZenDeskUrl + jwt;
                    if (!String.IsNullOrEmpty(returnTo))
                        result.URL += "&return_to=" + HttpUtility.UrlEncode(returnTo);
                    result.ErrorMessage = "";
                    result.ErrorOccurred = false;
                    AtlasLog.LogInformation("SUCCESS", gptwContext);
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorOccurred = true;
                result.ErrorMessage = "Unhandled exception:" + e.Message;
            }

            return result;
        }
        
        [NonAction]
        private String BuildZenDeskJWT(string clientId, string email, string name, string role, GptwLogContext parentGptwContext)
        {
            string jwt = "";

            //Microsoft Nuget JWT Component solution
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
                int timestamp = (int)t.TotalSeconds;
                var now = DateTime.UtcNow;
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new System.Security.Claims.ClaimsIdentity(new[]
                    {
                            new System.Security.Claims.Claim("iat", timestamp.ToString()),
                            new System.Security.Claims.Claim("jti", System.Guid.NewGuid().ToString()),
                            new System.Security.Claims.Claim("name", name),
                            new System.Security.Claims.Claim("email", email),
                            new System.Security.Claims.Claim("organization_id", clientId),
                            new System.Security.Claims.Claim("role", role),
                        }),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.ZenDeskSharedSecret)), SecurityAlgorithms.HmacSha256)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                jwt = tokenHandler.WriteToken(token);
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, parentGptwContext);
            }

            // JWT.NET Implementation
            //try
            //{
            //    TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            //    int timestamp = (int)t.TotalSeconds;
            //    var now = DateTime.UtcNow;

            //    var payload = new Dictionary<string, object>
            //    {
            //        { "iat", timestamp.ToString() },
            //        { "jti", System.Guid.NewGuid().ToString() },
            //        { "name", name },
            //        { "email", email }
            //    };
            //    string secret = appSettings.ZenDeskSharedSecret;

            //    IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
            //    IJsonSerializer serializer = new JsonNetSerializer();
            //    IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            //    IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            //    jwt = encoder.Encode(payload, secret);

            //}
            //catch (Exception e)
            //{
            //    appInsights.TrackEvent(String.Format("Portal/BuildZenDeskJWT error- {0} Exception caught.", e));
            //}

            return jwt;
        }

    }

    public class CreateHelpDeskTicketResult
    {
        public bool IsError;

    }
}