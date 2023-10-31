using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Model;
using Portal.Controllers;
using SharedProject2;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.IO.Compression;
using System.Net.Mime;
using Portal.Misc;
using System.Threading.Tasks;

//
// There are two main IsAuthorized methods to preface ALL CONTROLLER methods with.
//
// IsUserAuthorized and IsAuthorizedEmployee
//
// IsUserAuthorized(int clientIdBeingAccessed) is the main method to use for controllers which COULD be called by an end user. (it will also work for employees)
// Methods that are exclusively EMPLOYEE ONLY like CreateECR should call IsAuthorizedEmployee()
// So just call IsUserAuthorized passing in the clientId that the user is attempting to access information on. If they are accessing an engagementId you'll have to 
// determine which clientId that engagementId is associated with and then pass that clientId in. If the method returns false then log the error to Application Insights
// and return a 401 http response. See the example below. This method needs to be defined at the top of the controller as one of the first things to do.
// 
//if (!IsUserAuthorized(clientId))
//{
//    AtlasLog.LogError(String.Format("Portal/GetEngagementInfo: Not Authorized.token:{0}", authorizationToken));
//    Response.StatusCode = (int) HttpStatusCode.Unauthorized;
//    return result;
//}
//
//

namespace Recognition.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecognitionController : PortalControllerBase
    {
        public RecognitionController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        // Called from Client Recognition page
        [HttpGet("[action]")]
        public GetClientRecognitionInfoResult GetClientListRecognition(int clientId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetClientListRecognition");
            gptwContext.ClientId = clientId;

            GetClientRecognitionInfoResult result = new GetClientRecognitionInfoResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                ClientRecognitionInfoDetails = new List<ClientRecognitionInfoDetail>()
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
                    string url = appSettings.BusinessDataServicesURL + "/api/clientportal/getclientrecognitioninfo";

                    GetClientRecognitionInfoRequest pst = new GetClientRecognitionInfoRequest
                    {
                        token = appSettings.BusinessDataServicesToken,
                        clientid = clientId
                    };

                    String content = JsonConvert.SerializeObject(pst);

                    StringContent StuffToPost = new StringContent(content);
                    StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var postResult = hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json")).Result;
                    if (postResult.IsSuccessStatusCode)
                    {
                        string apiResponse = postResult.Content.ReadAsStringAsync().Result;
                        result.ClientRecognitionInfoDetails = JsonConvert.DeserializeObject<List<ClientRecognitionInfoDetail>>(apiResponse);
                        result.IsError = false;
                        result.ErrorStr = "";
                    }
                    else
                    {
                        result.IsError = true;
                        result.ErrorStr = "Error-http status " + postResult.StatusCode;
                        AtlasLog.LogError(String.Format("ERROR: failed- {0}", result.ErrorStr), gptwContext);
                        return result;
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

        // Email Toolkit info
        [HttpGet("[action]")]
        public EmailToolkitResult EmailToolkit(int clientId, int engagementId, string toEmailAddress, string referrer, string celebratelink, string usagelink, string pressreleaselink, string ccemail)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("EmailToolkit");
            gptwContext.ClientId = clientId;
            gptwContext.EngagementId = engagementId;
            EmailToolkitResult result = new EmailToolkitResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
            };

            try
            {
                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                string msgSubject = "Inside: Your Certification badge and assets"; 
                var repo = new AtlasOperationsDataRepository(appSettings.MongoDBConnectionString, appSettings.MongoLockAzureStorageConnectionString);
                ECRV2 ecrv2 = repo.RetrieveReadOnlyECRV2(engagementId);
                EmailTracking emailTracking = repo.CreateNewEmailTracking(ecrv2.ClientId, ecrv2.EngagementId,
                                    msgSubject, EmailType.SendToolkitEmail, DateTime.UtcNow, toEmailAddress);
                // Assume that it failed until we know otherwise
                emailTracking.IsError = true;

                repo.SaveEmailTracking(emailTracking); // The call to the Atlas Email Content Web API requires that the emailtracking record exists so it can pull the cid so we must save before calling
                AtlasToolKitEmailContentCallResult setresult = CallAtlasToGetToolKitEmailContent( gptwContext, emailTracking.Id.ToString()).Result;

                string msgBody = setresult.EmailContent;

                // set up the mailer
                SmtpClient mailer = new SmtpClient(appSettings.LargeSmtpHost, System.Convert.ToInt32(appSettings.SmtpPort));
                mailer.Credentials = new System.Net.NetworkCredential(appSettings.SmtpUsername, appSettings.SmtpPassword);
                string fromAddress = appSettings.SmtpFromAddress;
                msgBody = msgBody.Replace("{PORTALUSERNAME}", referrer);

                emailTracking.Body = msgBody;
                repo.SaveEmailTracking(emailTracking);

                MailMessage mesg = new MailMessage();
                MailAddress from = new MailAddress(fromAddress, "Great Place to Work");
                MailAddress receiver = new MailAddress(toEmailAddress);
                mesg.From = from;
                mesg.To.Add(receiver);
                mesg.Subject = msgSubject;
                mesg.Body = msgBody;
                mesg.IsBodyHtml = true;
                mesg.CC.Add(ccemail);

                // Build a stream containing the badge
                ImageBuilder imageBuilder = new ImageBuilder(repo, gptwContext, AtlasLog);
                Stream badgeStream = imageBuilder.createBadgeZipStream(engagementId);

                string outputFilename = "Great Place To Work Certification Badges.zip";

                // Add attachment(s)
                mesg.Attachments.Add(CreateAttachmentFromStream(badgeStream, outputFilename, MediaTypeNames.Application.Zip));
                // mesg.Attachments.Add(CreateAttachmentFromUri(pressreleaselink, "Sample_Certification_Press_Release.docx", MediaTypeNames.Application.Octet));

                try
                {
                    mailer.Send(mesg);
                    result.IsError = false;
                    result.ErrorStr = "";

                    UserEventEnums.UserType userTypeEnum = UserEventEnums.UserType.End_User;
                    if (IsGPTWEmployee(gptwContext))
                        userTypeEnum = UserEventEnums.UserType.Employee;

                    repo.SaveUserEvent(UserEventEnums.Source.Portal, UserEventEnums.Name.Toolkit_Email_Friend,
                                   gptwContext.ClientId, gptwContext.EngagementId, userTypeEnum,
                                   gptwContext.Email, gptwContext.SessionId, "Friends email:" + toEmailAddress);

                    System.Threading.Thread.Sleep(5000);

                    // Mark that the toolkit has been shared
                    if (userTypeEnum == UserEventEnums.UserType.End_User)
                        this.SetCustomerActivationStatus(engagementId, EngagementUpdateField.CustomerActivationShareToolkit);

                    emailTracking.IsError = false;
                    repo.SaveEmailTracking(emailTracking);

                    return result;
                }
                catch (Exception e)
                {
                    AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                    result.IsError = true;
                    result.ErrorStr = "Unhandled exception:" + e.Message;
                    emailTracking.ErrorMessage = result.ErrorStr;
                    repo.SaveEmailTracking(emailTracking);
                }

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.IsError = true;
                result.ErrorStr = "Unhandled exception:" + e.Message;
            }

            System.Threading.Thread.Sleep(5000);

            return result;

        }

        [NonAction]
        private async Task<AtlasToolKitEmailContentCallResult> CallAtlasToGetToolKitEmailContent(GptwLogContext parentGptwContext, string id)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("CallAtlasToGetToolKitEmailContent", parentGptwContext);

            AtlasToolKitEmailContentCallResult result = new AtlasToolKitEmailContentCallResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                EmailContent = ""
            };

            try
            {
                HttpResponseMessage getResult;

                using (var hc = clientFactory.CreateClient())
                {
                    string url = String.Format("{0}/emailContent/{1}/{2}/{3}/{4}", appSettings.ReportingURL, EmailType.SendToolkitEmail, id, "1", "1");
                    getResult = await hc.GetAsync(url);
                }

                if (getResult.IsSuccessStatusCode)
                {
                    AtlasLog.LogInformation(String.Format("Returned Successfully from: AtlasSetStatus WebAPI."), gptwContext);

                    result.EmailContent = getResult.Content.ReadAsStringAsync().Result;

                    //result = JsonConvert.DeserializeObject<AtlasCallSetStatusResult>(apiResponse);

                    return result;
                }
                else
                {
                    AtlasLog.LogError(String.Format("ERROR:CallAtlasToGetToolKitEmailContent failed with an http status code of {0}.", getResult.StatusCode), gptwContext);
                    result.ErrorStr = "CallAtlasToGetToolKitEmailContent failed with an http status code of " + getResult.StatusCode;
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorStr = "CallAtlasToGetToolKitEmailContent failed with an unhandled error of: " + e.Message;
            }

            return result;
        }

        //// Email Toolkit info
        //[HttpGet("[action]")]
        //public EmailToolkitResult EmailToolkit(int clientId, string toEmailAddress, string referrer, string celebratelink, string usagelink, string badgelink)
        //{
        //    GptwLogContext gptwContext = GetNewGptwLogContext("GetClientListRecognition");
        //    gptwContext.ClientId = clientId;

        //    EmailToolkitResult result = new EmailToolkitResult
        //    {
        //        IsError = true,
        //        ErrorStr = "A general error has occurred.",
        //    };

        //    try
        //    {
        //        OperationsEmailType emailType = OperationsEmailType.SendToolkitEmail;
        //        var repo = new AtlasOperationsDataRepository(appSettings.MongoDBConnectionString, appSettings.MongoLockAzureStorageConnectionString);
        //        OperationsEmail emailTemplate = repo.RetrieveOperationsEmails(emailType);
        //        if (emailTemplate == null)
        //        {
        //            result.IsError = true;
        //            result.ErrorStr = "No email template found for SendToolkitEmail";
        //            return result;
        //        }

        //        // set up the mailer
        //        SmtpClient mailer = new SmtpClient(appSettings.SmtpHost, System.Convert.ToInt32(appSettings.SmtpPort));
        //        mailer.Credentials = new System.Net.NetworkCredential(appSettings.SmtpUsername, appSettings.SmtpPassword);
        //        string msgSubject = emailTemplate.SubjectForEmail.Replace("{TM}", "™");
        //        string msgBody = emailTemplate.HtmlForEmail.Replace("{TM}", "™");
        //        string fromAddress = appSettings.SmtpFromAddress;

        //        msgBody = msgBody.Replace("{CelebrateLink}", celebratelink);
        //        msgBody = msgBody.Replace("{UsageLink}", usagelink);
        //        msgBody = msgBody.Replace("{BadgeLink}", badgelink);


        //        OperationsEmail uberTemplate = repo.RetrieveOperationsEmails(OperationsEmailType.Template);
        //        var body = uberTemplate.HtmlForEmail;
        //        string replacedContent = body.Replace("$$EMAIL_CONTENT$$", msgBody);

        //        MailMessage mesg = new MailMessage(fromAddress, toEmailAddress, msgSubject, replacedContent);

        //        mesg.IsBodyHtml = true;

        //        try
        //        {
        //            mailer.Send(mesg);
        //            result.IsError = false;
        //            result.ErrorStr = "";
        //            return result;
        //        }
        //        catch (Exception e)
        //        {
        //            AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
        //            result.IsError = true;
        //            result.ErrorStr = "Unhandled exception:" + e.Message;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
        //        result.IsError = true;
        //        result.ErrorStr = "Unhandled exception:" + e.Message;
        //    }

        //    return result;
        //}

        /// <summary>
        /// Create an email attachment from a file located at the given uri
        /// </summary>
        /// <param name="uri">Location of the file</param>
        /// <param name="filename">Name to give the file in the attachment</param>
        /// <param name="mimetype">Mimetype of the file</param>
        /// <returns></returns>
        /// <remarks>Exception handling to be managed by caller</remarks>
        Attachment CreateAttachmentFromUri(string uri, string filename, string mimetype)
        {

            MemoryStream stream = DownloadFileToStream(uri);
            Attachment attach = CreateAttachmentFromStream(stream, filename, mimetype);
            return attach;
        }

        Attachment CreateAttachmentFromStream(Stream stream, string filename, string mimetype)
        {

            Attachment attach = new Attachment(stream, filename, mimetype);
            attach.TransferEncoding = TransferEncoding.Base64;
            return attach;
        }

        /// <summary>
        /// Download a file at the given uri and return as a stream
        /// </summary>
        /// <param name="uri">Location of file</param>
        /// <returns>Stream</returns>
        /// <remarks>Exception handling to be managed by caller</remarks>
        MemoryStream DownloadFileToStream(string uri)
        {
            System.IO.MemoryStream stream = null;
            using (WebClient myClient = new WebClient())
            {
                byte[] bytes = myClient.DownloadData(uri);
                stream = new MemoryStream(bytes);
            }
            return stream;
        }
    }
}

