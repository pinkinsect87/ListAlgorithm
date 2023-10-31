using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Model;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Net.Http.Headers;
using Portal.Controllers;
using System.Globalization;
using System.Text.RegularExpressions;
using CultureSurveyShared;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Users.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : PortalControllerBase
    {
        const int ERROR_ID_NOT_DEFINED = 0;
        const int ERROR_ID_CONTACT_EMAIL_IN_USE = 1;
        const int ERROR_ID_CONTACT_EMAIL_MATCHES_GPTW = 2;

        public UsersController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        [HttpGet("[action]")]
        public GetCompanyUsersResult GetCompanyUsers(int clientId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetCompanyUsers");
            gptwContext.ClientId = clientId;

            GetCompanyUsersResult result = new GetCompanyUsersResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                PortalContacts = new List<PortalContact>()
            };

            try
            {
                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                using (var hc = clientFactory.CreateClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                    string url = appSettings.ReportingURL + "/orchestration/getportalcontacts";

                   // url = "http://localhost:7779/orchestration/getportalcontacts";

                    GetCompanyUsersRequest pst = new GetCompanyUsersRequest
                    {
                        ClientId = clientId,
                        SessionId = gptwContext.SessionId,
                        CallerEmail = gptwContext.Email
                    };

                    AtlasLog.LogInformation(String.Format("Calling: Atlas GetPortalContacts WebAPI. {0}", JsonConvert.SerializeObject(pst)), gptwContext);

                    pst.Username = appSettings.AtlasApiUserName;
                    pst.Password = appSettings.AtlasApiPassword;

                    String content = JsonConvert.SerializeObject(pst);

                    StringContent StuffToPost = new StringContent(content);
                    StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var postResult = hc.PostAsync(url, new StringContent(content, System.Text.UnicodeEncoding.UTF8, "application/json")).Result;

                    if (postResult.IsSuccessStatusCode)
                    {
                        AtlasLog.LogInformation(String.Format("Returned Successfully from: Atlas GetPortalContacts WebAPI."), gptwContext);
                        string apiResponse = postResult.Content.ReadAsStringAsync().Result;
                        AtlasLog.LogInformation(String.Format("Returned result {0}", apiResponse), gptwContext);
                        result = JsonConvert.DeserializeObject<GetCompanyUsersResult>(apiResponse);
                    }
                    else
                    {
                        result.IsError = true;
                        result.ErrorStr = "Error-http status " + postResult.StatusCode;
                        AtlasLog.LogError(String.Format("ERROR: failed- {0}", result.ErrorStr), gptwContext);
                    }
                }

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.IsError = true;
                result.ErrorStr = "Unhandled exception:" + e.Message;
            }

            return result;
        }

        [HttpGet("[action]")]
        public AddUpdateContactResult AddCompanyUser(int clientId, string firstName, string lastName, string email, bool achievementNotification)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("AddCompanyUser");
            gptwContext.ClientId = clientId;

            AddUpdateContactResult result = new AddUpdateContactResult
            {
                IsError = true,
                ErrorId = ERROR_ID_NOT_DEFINED,
                ErrorStr = "A general error has occurred."
            };

            try
            {
                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || String.IsNullOrEmpty(email))
                {
                    result.IsError = true;
                    result.ErrorStr = String.Format("<DISPLAYTOUSER>First names, last names and email addresses are all required.", email);
                    AtlasLog.LogError(String.Format("ERROR: failed- {0}", result.ErrorStr), gptwContext);
                    return result;
                }

                if (!IsValidEmail(email))
                {
                    result.IsError = true;
                    result.ErrorStr = "<DISPLAYTOUSER>Invalid email. Use only A-Z 0-9 ~!$%^&*_=+}{'?-.@";
                    AtlasLog.LogError(String.Format("ERROR: failed- {0}", result.ErrorStr), gptwContext);
                    return result;
                }

                using (var hc = clientFactory.CreateClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                    string url = appSettings.ReportingURL + "/orchestration/createportalcontact";

                    CreateUpdateUserRequest pst = new CreateUpdateUserRequest
                    {
                        ClientId = clientId,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        AchievementNotification = achievementNotification,
                        SessionId = gptwContext.SessionId,
                        CallerEmail = gptwContext.Email
                    };

                    AtlasLog.LogInformation(String.Format("Calling: Atlas CreatePortalContact WebAPI. {0}", JsonConvert.SerializeObject(pst)), gptwContext);

                    pst.Username = appSettings.AtlasApiUserName;
                    pst.Password = appSettings.AtlasApiPassword;

                    String content = JsonConvert.SerializeObject(pst);

                    StringContent StuffToPost = new StringContent(content);
                    StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var postResult = hc.PostAsync(url, new StringContent(content, System.Text.UnicodeEncoding.UTF8, "application/json")).Result;

                    if (postResult.IsSuccessStatusCode)
                    {
                        AtlasLog.LogInformation(String.Format("Returned Successfully from: Atlas Createportalcontact WebAPI."), gptwContext);
                        string apiResponse = postResult.Content.ReadAsStringAsync().Result;
                        AtlasLog.LogInformation(String.Format("Returned result {0}", apiResponse), gptwContext);
                        result = JsonConvert.DeserializeObject<AddUpdateContactResult>(apiResponse);
                    }
                    else
                    {
                        result.IsError = true;
                        result.ErrorStr = "Error-http status " + postResult.StatusCode;
                        AtlasLog.LogError(String.Format("ERROR: failed- {0}", result.ErrorStr), gptwContext);
                    }
                }

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.IsError = true;
                result.ErrorStr = "Unhandled exception:" + e.Message;
            }

            return result;
        }

        [HttpGet("[action]")]
        public GenericResult DeleteCompanyUser(int clientId, string email)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("DeleteCompanyUser");
            gptwContext.ClientId = clientId;

            GenericResult result = new GenericResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred."
            };

            try
            {
                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                using (var hc = clientFactory.CreateClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                    string url = appSettings.ReportingURL + "/orchestration/removeportalcontact";

                    DeleteUserRequest pst = new DeleteUserRequest
                    {
                        ClientId = clientId,
                        Email = email,
                        SessionId = gptwContext.SessionId,
                        CallerEmail = gptwContext.Email
                    };

                    AtlasLog.LogInformation(String.Format("Calling: Atlas RemovePortalContact WebAPI. {0}", JsonConvert.SerializeObject(pst)), gptwContext);

                    pst.Username = appSettings.AtlasApiUserName;
                    pst.Password = appSettings.AtlasApiPassword;

                    String content = JsonConvert.SerializeObject(pst);

                    StringContent StuffToPost = new StringContent(content);
                    StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var postResult = hc.PostAsync(url, new StringContent(content, System.Text.UnicodeEncoding.UTF8, "application/json")).Result;

                    if (postResult.IsSuccessStatusCode)
                    {
                        AtlasLog.LogInformation(String.Format("Returned Successfully from: Atlas RemovePortalContact WebAPI."), gptwContext);
                        string apiResponse = postResult.Content.ReadAsStringAsync().Result;
                        AtlasLog.LogInformation(String.Format("Returned result {0}", apiResponse), gptwContext);
                        result = JsonConvert.DeserializeObject<GenericResult>(apiResponse);
                    }
                    else
                    {
                        result.IsError = true;
                        result.ErrorStr = "Error-http status " + postResult.StatusCode;
                        AtlasLog.LogError(String.Format("ERROR: failed- {0}", result.ErrorStr), gptwContext);
                    }
                }

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.IsError = true;
                result.ErrorStr = "Unhandled exception:" + e.Message;
            }

            return result;
        }

        [HttpGet("[action]")]
        public AddUpdateContactResult UpdateCompanyUser(int clientId, string firstName, string lastName, string email, bool achievementNotification)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("UpdateCompanyUser");
            gptwContext.ClientId = clientId;

            AddUpdateContactResult result = new AddUpdateContactResult
            {
                IsError = true,
                ErrorId = ERROR_ID_NOT_DEFINED,
                ErrorStr = "A general error has occurred."
            };

            try
            {
                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName)) { 
                    result.ErrorStr = String.Format("<DISPLAYTOUSER>First and last names are required.");
                    AtlasLog.LogError(String.Format("ERROR: failed- {0}", result.ErrorStr), gptwContext);
                    return result;
                }

                using (var hc = clientFactory.CreateClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                    string url = appSettings.ReportingURL + "/orchestration/updateportalcontact";

                    CreateUpdateUserRequest pst = new CreateUpdateUserRequest
                    {
                        ClientId = clientId,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        AchievementNotification = achievementNotification,
                        SessionId = gptwContext.SessionId,
                        CallerEmail = gptwContext.Email
                    };

                    AtlasLog.LogInformation(String.Format("Calling: Atlas UpdatePortalContact WebAPI. {0}", JsonConvert.SerializeObject(pst)), gptwContext);

                    pst.Username = appSettings.AtlasApiUserName;
                    pst.Password = appSettings.AtlasApiPassword;

                    String content = JsonConvert.SerializeObject(pst);

                    StringContent StuffToPost = new StringContent(content);
                    StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var postResult = hc.PostAsync(url, new StringContent(content, System.Text.UnicodeEncoding.UTF8, "application/json")).Result;

                    if (postResult.IsSuccessStatusCode)
                    {
                        AtlasLog.LogInformation(String.Format("Returned Successfully from: Atlas UpdatePortalContact WebAPI."), gptwContext);
                        string apiResponse = postResult.Content.ReadAsStringAsync().Result;
                        AtlasLog.LogInformation(String.Format("Returned result {0}", apiResponse), gptwContext);
                        GenericResult res = JsonConvert.DeserializeObject<GenericResult>(apiResponse);
                        result.IsError = res.IsError;
                        result.ErrorStr = res.ErrorStr;
                    }
                    else
                    {
                        result.IsError = true;
                        result.ErrorStr = "Error-http status " + postResult.StatusCode;
                        AtlasLog.LogError(String.Format("ERROR: failed- {0}", result.ErrorStr), gptwContext);
                    }
                }

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.IsError = true;
                result.ErrorStr = "Unhandled exception:" + e.Message;
            }

            return result;
        }

        [NonAction]
        private static bool IsValidEmail(string email)
        {

            try
            {
                return CommonValidations.IsEmailAddressValid(email);
            }
            catch (Exception e)
            {
                return false;
            }


            //if (string.IsNullOrWhiteSpace(email))
            //    return false;

            //try
            //{
            //    // Normalize the domain
            //    email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
            //                          RegexOptions.None, TimeSpan.FromMilliseconds(200));

            //    // Examines the domain part of the email and normalizes it.
            //    string DomainMapper(Match match)
            //    {
            //        // Use IdnMapping class to convert Unicode domain names.
            //        var idn = new IdnMapping();

            //        // Pull out and process domain name (throws ArgumentException on invalid)
            //        var domainName = idn.GetAscii(match.Groups[2].Value);

            //        return match.Groups[1].Value + domainName;
            //    }
            //}
            //catch (RegexMatchTimeoutException e)
            //{
            //    return false;
            //}
            //catch (ArgumentException e)
            //{
            //    return false;
            //}


            //    try
            //    {
            //        return Regex.IsMatch(email,
            //            @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
            //            @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
            //            RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            //    }
            //    catch (RegexMatchTimeoutException)
            //    {
            //        return false;
            //    }
        }

    }
}

