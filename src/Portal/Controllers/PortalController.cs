using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SharedProject2;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Portal.Model;
using Portal.Misc;
using MongoDB.Bson;
using ZendeskApi_v2.Models.Groups;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.Extensions.FileProviders;
using CultureSurveyShared;
using Aspose.Words.Lists;
using Azure.Messaging.EventGrid.SystemEvents;

//using System.Security.Claims;
//using JWT.Algorithms;
//using JWT;
//using JWT.Serializers;

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
// if (!SharedControllerMethods.IsUserAuthorized(Request.Headers, User.Claims, this.ci, clientId))
// {
//  this.ci.telemetry.TrackEvent("FindCurrentEngagementInfo:Not Authorized. token:" + Request.Headers.FirstOrDefault(h => h.Key.Equals("Authorization")).Value);
//  Response.StatusCode = (int)HttpStatusCode.Unauthorized;
//  return result;
// }
//
// Adding fake comment to force the build number to change to test app refresh

namespace Portal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortalController : PortalControllerBase
    {
        const int ERROR_ID_NOT_DEFINED = 0;
        const int ERROR_ID_CONTACT_EMAIL_IN_USE = 1;
        const int ERROR_ID_CONTACT_EMAIL_MATCHES_GPTW = 2;
        const int ERROR_ID_FAILED_TO_FIND_ECRV2 = 3;

        public PortalController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        // Called with a ClientId in the end user case where they are logging in and we don't know which engagementId to route them to
        // Called with an EngagementId when either:
        //      1. an employee has a link with an engagementId
        //      2. or an end user has bookmarked the link

        [HttpGet("[action]")]
        public FindCurrentEngagementInfoResult FindCurrentEngagementInfo(string propertyName, int property)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("FindCurrentEngagementInfo");

            FindCurrentEngagementInfoResult result = new FindCurrentEngagementInfoResult
            {
                ErrorOccurred = true,
                ErrorMessage = "A general error has occurred.",
                ErrorId = ERROR_ID_NOT_DEFINED,
                FoundActiveEngagement = false,
                EngagementId = -1,
                CertificationStatus = ""
            };

            // Validate parameters
            if (propertyName != "engagementId" && propertyName != "clientId")
            {
                AtlasLog.LogError(String.Format("Invalid propertyName:{0}", propertyName), gptwContext);
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return result;
            }

            try
            {
                ECRV2 ecrv2;
                int clientId = -1;
                int engagementId = -1;

                if (propertyName == "engagementId")
                {
                    engagementId = property;
                    ecrv2 = this.AORepository.RetrieveReadOnlyECRV2(engagementId);
                    clientId = ecrv2.ClientId;
                }
                else
                {
                    clientId = property;
                }

                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                if (propertyName == "clientId")
                {
                    engagementId = FindEngagementId(clientId, "CURRENT", gptwContext);
                    ecrv2 = this.AORepository.RetrieveReadOnlyECRV2(engagementId);

                    if (ecrv2 == null)
                    {
                        result.ErrorOccurred = true;
                        result.FoundActiveEngagement = false;
                        result.ErrorMessage = "Failed to retrieve ECRV2";
                        result.ErrorId = ERROR_ID_FAILED_TO_FIND_ECRV2;
                        return result;
                    }

                    result.EngagementId = engagementId;
                    List<CountryData> certcountries1 = this.AORepository.FindCertificationCountries(ecrv2);
                    if (certcountries1.Count > 0)
                    {
                        result.CertificationStatus = NormalizeCertificationStatus(certcountries1[0].CertificationStatus);
                    }
                    result.ErrorOccurred = false;
                    result.FoundActiveEngagement = true;
                    result.ErrorMessage = "";

                    result.ClientId = clientId;
                    result.EngagementId = engagementId;
                    result.ClientName = ecrv2.ClientName;

                    return result;
                }

                ecrv2 = this.AORepository.RetrieveReadOnlyECRV2(engagementId);

                if (ecrv2 == null)
                {
                    result.ErrorOccurred = true;
                    result.FoundActiveEngagement = false;
                    result.ErrorMessage = "Failed to retrieve ECRV2";
                    result.ErrorId = ERROR_ID_FAILED_TO_FIND_ECRV2;
                    return result;
                }

                result.EngagementId = engagementId;
                List<CountryData> certcountries = this.AORepository.FindCertificationCountries(ecrv2);
                if (certcountries.Count > 0)
                {
                    result.CertificationStatus = NormalizeCertificationStatus(certcountries[0].CertificationStatus);
                }
                result.ErrorOccurred = false;
                result.FoundActiveEngagement = true;
                result.ErrorMessage = "";

                result.ClientId = clientId;
                result.ClientName = ecrv2.ClientName;

                return result;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }

        // Called with a specific engagementId in order to return specific details about the engagement
        [HttpGet("[action]")]
        public GetEngagementInfoResult GetEngagementInfo(int engagementId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetEngagementInfo");
            gptwContext.EngagementId = engagementId;

            GetEngagementInfoResult result = new GetEngagementInfoResult
            {
                ErrorOccurred = true,
                ErrorMessage = "A general error has occurred.",
                FoundActiveEngagement = false,
                EInfo = new EngagementInfo()
            };

            try
            {
                int clientId = -1;
                ECRV2 ecrv2 = this.AORepository.RetrieveReadOnlyECRV2(engagementId);
                if (ecrv2 == null)
                {
                    result.ErrorOccurred = true;
                    result.FoundActiveEngagement = false;
                    result.ErrorMessage = "Failed to retrieve ECRV2";
                    return result;
                }

                ECRV2 latestNonAbandonedEcr = this.AORepository.GetLatestNotAbandonedECR(ecrv2.ClientId);
                bool isLatestECR = false;
                if (latestNonAbandonedEcr == null)
                    isLatestECR = true;
                else if (ecrv2.CreatedDate >= latestNonAbandonedEcr.CreatedDate)
                    isLatestECR = true;

                clientId = ecrv2.ClientId;

                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                result.EInfo.ClientId = clientId.ToString();
                result.EInfo.EngagementId = engagementId;
                List<CountryData> certcountries = this.AORepository.FindCertificationCountries(ecrv2);
                if (certcountries.Count > 0)
                {
                    result.EInfo.CertificationStatus = NormalizeCertificationStatus(certcountries[0].CertificationStatus);
                }
                result.ErrorOccurred = false;
                result.FoundActiveEngagement = true;
                result.ErrorMessage = "";

                result.EInfo.isMNCCID = false;
                List<ECRV2> ecrv2s = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);
                if (ecrv2s != null && ecrv2s.Count > 0 && ecrv2s.Any(p => p.ECR.Countries.Count > 1))
                    result.EInfo.isMNCCID = true;

                //result.EInfo.isMNCCID = this.AORepository.IsCIDAnMNC(clientId);
                result.EInfo.ClientName = ecrv2.ClientName;
                result.EInfo.Tier = ecrv2.Tier;
                result.EInfo.JourneyStatus = ECRV2.GetEnumDescription(ecrv2.JourneyStatus);
                result.EInfo.TrustIndexSSOLink = ecrv2.ECR.TrustIndexSSOLink;
                result.EInfo.TrustIndexStatus = ecrv2.ECR.TrustIndexStatus;
                result.EInfo.TrustIndexSourceSystemSurveyId = ecrv2.ECR.TrustIndexSourceSystemSurveyId;
                result.EInfo.CultureAuditSSOLink = ecrv2.ECR.CultureAuditSSOLink;
                result.EInfo.CultureAuditStatus = ecrv2.ECR.CultureAuditStatus;
                result.EInfo.CultureAuditSourceSystemSurveyId = ecrv2.ECR.CultureAuditSourceSystemSurveyId;
                result.EInfo.CultureBriefSSOLink = ecrv2.ECR.CultureBriefSSOLink;
                result.EInfo.CultureBriefStatus = ecrv2.ECR.CultureBriefStatus;
                result.EInfo.CultureBriefSourceSystemSurveyId = ecrv2.ECR.CultureBriefSourceSystemSurveyId;
                result.EInfo.CompanyName = ecrv2.ClientName;
                result.EInfo.ReviewCenterPublishedLink = certcountries != null && certcountries.Count > 0 ? certcountries[0].ProfilePublishedLink : "";
                result.EInfo.IsLatestECR = isLatestECR;
                result.EInfo.IsAbandoned = ecrv2.IsAbandoned;
                result.EInfo.AffiliateId = ecrv2.AffiliateId;
                //New MNC Work Jan 23,2023
                result.EInfo.ECRCreationDate = ecrv2.CreatedDate.ToString("MM/yyyy");
                string SubstrCountryCode = "";
                List<CountryData> ListECRcountriesbyEngagementID = new List<CountryData>();
                ListECRcountriesbyEngagementID = ecrv2.ECR.Countries;
                List<string> ListCountryCode = new List<string>();
                //ListECRcountriesbyEngagementID.Sort();
                result.EInfo.CountriesCount = ListECRcountriesbyEngagementID.Count;
                if (ListECRcountriesbyEngagementID.Count > 0)
                {
                    for (int i = 0; i < ListECRcountriesbyEngagementID.Count; i++)
                    {

                        //if (ListECRcountriesbyEngagementID[i].IsApplyingForCertification == "Yes")
                        //{
                        //SubstrCountryCode += ListECRcountriesbyEngagementID[i].CountryCode.ToString() + ", ";
                        ListCountryCode.Add(ListECRcountriesbyEngagementID[i].CountryCode.ToString());
                        //}
                        //else
                        //{
                        //    SubstrCountryCode += ListECRcountriesbyEngagementID[i].CountryCode.ToString() + "*, ";
                        //}

                    }
                }
                List<Country> ListCountryData = this.AORepository.GetCountryByCountryCodeList(ListCountryCode);

                for (int i = 0; i < ListECRcountriesbyEngagementID.Count; i++)
                {
                    for (int j = 0; i < ListCountryData.Count; j++)
                    {
                        if (ListECRcountriesbyEngagementID[i].CountryCode == ListCountryData[j].CountryCode)
                        {
                            if (ListECRcountriesbyEngagementID[i].IsApplyingForCertification == "Yes")
                            {
                                SubstrCountryCode += ListCountryData[j].CountryName.ToString() + ", ";
                            }
                            else
                            {
                                SubstrCountryCode += ListCountryData[j].CountryName.ToString() + "*, ";
                            }
                            break;
                        }

                    }
                }


                SubstrCountryCode = SubstrCountryCode.Substring(0, SubstrCountryCode.Length - 2);
                // string ice = "";
                //ListCountryData.ForEach(ice => ListECRcountriesbyEngagementID.Add(ice));
                result.EInfo.ECRCountries = SubstrCountryCode;


                // TFS Task#6686 says that going forward the date will always be Aug 2nd of each year.
                // Here we will start displaying the new date on August 3rd of each year.

                DateTime currentDate = DateTime.UtcNow;
                int year = currentDate.Year;
                DateTime August3nd = new DateTime(year, 8, 3);
                if (DateTime.Compare(currentDate, August3nd) >= 0)
                    year += 1;

                result.EInfo.BestCompDeadline = "Aug 2, " + year.ToString();

                result.curCertInfo.certificationExpiryDate = "";
                result.curCertInfo.empResultsDate = "";
                result.curCertInfo.reportDownloadsDate = "";

                if (result.EInfo.CertificationStatus == "certified")
                {
                    // for certified engagements, get the current certification details
                    GetCertificationDetails(result, ecrv2);
                }
                else
                {
                    // for in-progress engagements, get the previous engagement's certification details, if there was one
                    int previousEID = FindPreviousCertifiedEngagementId(clientId, engagementId, gptwContext);
                    if (previousEID > 0)
                    {
                        ECRV2 prevEcrv2 = this.AORepository.RetrieveReadOnlyECRV2(previousEID);
                        GetCertificationDetails(result, prevEcrv2);
                        result.curCertInfo.trustIndexSSOLink = prevEcrv2.ECR.TrustIndexSSOLink;
                    }
                }

                if (!IsGPTWEmployee(gptwContext)) // End Users Portal Login event
                {
                    ECRV2 writeableEcrv2 = this.AORepository.RetrieveReadWriteECRV2(engagementId);
                    writeableEcrv2.PortalEngagementPageViewed();
                    this.AORepository.SaveECRV2(ref writeableEcrv2, true);
                }

                return result;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }

        [HttpGet("[action]")]
        public FindMostRecentCertificationEIDResult FindMostRecentCertificationEID(int clientId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("FindMostRecentCertificationEID");
            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = clientId;
            }
            catch (Exception) { }

            FindMostRecentCertificationEIDResult result = new FindMostRecentCertificationEIDResult
            {
                ErrorOccurred = true,
                ErrorMessage = "A general error has occurred.",
                FoundCertificationEngagement = false,
                EngagementId = -1,
                CertDate = ""
            };

            try
            {
                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                List<ECRV2> AllECRV2sForClient = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);

                // Get a descending list of certified engagements, regardless of country of certification
                ECRV2 mostRecentCertifiedECR = (from e in AllECRV2sForClient.AsQueryable()
                                                where (!e.IsAbandoned && String.Equals(this.AORepository.FindCertificationCountries(e)[0].CertificationStatus, "certified", StringComparison.OrdinalIgnoreCase))
                                                select e).FirstOrDefault();


                if (mostRecentCertifiedECR is null)
                {
                    result.ErrorMessage = "";
                    result.ErrorOccurred = false;
                    result.FoundCertificationEngagement = false;
                    AtlasLog.LogErrorStringOnly(String.Format("No certification ecr found for cid:", clientId), gptwContext);
                    return result;
                }

                result.ErrorMessage = "";
                result.ErrorOccurred = false;
                result.FoundCertificationEngagement = true;
                result.EngagementId = mostRecentCertifiedECR.EngagementId;
                result.AffiliateId = mostRecentCertifiedECR.AffiliateId;

                // Get the certification details from the certified country data
                // TODO Assuming only one country is certified
                CountryData certifiedCountry = (from c in this.AORepository.FindCertificationCountries(mostRecentCertifiedECR)
                                                where (String.Equals(c.CertificationStatus, "certified", StringComparison.OrdinalIgnoreCase))
                                                select c).FirstOrDefault();

                result.CertDate = ((DateTime)certifiedCountry.CertificationDate).ToShortDateString();
                result.CertExpiryDate = ((DateTime)certifiedCountry.CertificationExpiryDate).ToShortDateString();
                result.CertCountryCode = certifiedCountry.CountryCode;

                try
                {
                    UserEventEnums.UserType userTypeEnum = UserEventEnums.UserType.End_User;
                    if (IsGPTWEmployee(gptwContext))
                        userTypeEnum = UserEventEnums.UserType.Employee;
                    this.AORepository.SaveUserEvent(UserEventEnums.Source.Portal, UserEventEnums.Name.Toolkit_Page_View,
                                   clientId, result.EngagementId, userTypeEnum, gptwContext.Email, gptwContext.SessionId, "");
                }
                catch (Exception e)
                {
                    AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught when attempting to post a user event."), e, gptwContext);
                }

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }

        [HttpGet("[action]")]
        public AppConfigDetailsResult GetAppConfigDetails(string appVersion)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetAppConfigDetails");

            AppConfigDetailsResult result = new AppConfigDetailsResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                IsAppMaintenanceMode = appSettings.IsAppMaintenanceMode,
                AppVersion = appSettings.AppBuildNumber
            };

            try
            {
                if (appVersion != null && appVersion != "null" && appVersion != result.AppVersion)
                {
                    AtlasLog.LogInformation(String.Format("Browser Refresh Event Detected. User has version:{0} which have been replaced by version:{1}. Forcing a browser refresh now.", appVersion, result.AppVersion), gptwContext);
                }

                result.IsError = false;
                result.ErrorStr = "";
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
        public PostUserEventResult PostUserEvent(UserEventEnums.Name userEventEnumName, string cid, string eid)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("PostUserEvent");
            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = Int32.Parse(cid);
                if (gptwContext.EngagementId <= 0)
                    gptwContext.EngagementId = Int32.Parse(eid);
            }
            catch (Exception) { }

            PostUserEventResult result = new PostUserEventResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                userEventEnumName = userEventEnumName
            };

            try
            {
                if (!IsUserAuthorized(gptwContext, Int32.Parse(cid)))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                UserEventEnums.UserType userTypeEnum = UserEventEnums.UserType.End_User;
                if (IsGPTWEmployee(gptwContext))
                    userTypeEnum = UserEventEnums.UserType.Employee;

                string AdditionalInfo = "";

                this.AORepository.SaveUserEvent(UserEventEnums.Source.Portal, userEventEnumName,
                               gptwContext.ClientId, gptwContext.EngagementId, userTypeEnum,
                               gptwContext.Email, gptwContext.SessionId, AdditionalInfo);

                // Set customer activation status, if this is an activation event, and not by an employee
                if (userTypeEnum != UserEventEnums.UserType.Employee)
                {
                    switch (userEventEnumName)
                    {
                        case UserEventEnums.Name.Go_To_TI:
                            this.SetCustomerActivationStatus(Int32.Parse(eid), EngagementUpdateField.CustomerActivationEmprisingAccess);
                            break;
                        case UserEventEnums.Name.Report_Download:
                            this.SetCustomerActivationStatus(Int32.Parse(eid), EngagementUpdateField.CustomerActivationReportDownload);
                            break;
                        case UserEventEnums.Name.Toolkit_Download_BadgeSVG:
                        case UserEventEnums.Name.Toolkit_Download_BadgeJPG:
                        case UserEventEnums.Name.Toolkit_Download_BadgePNG:
                        case UserEventEnums.Name.Toolkit_Download_BadgeZIP:
                            this.SetCustomerActivationStatus(Int32.Parse(eid), EngagementUpdateField.CustomerActivationBadgeDownload);
                            break;
                        case UserEventEnums.Name.Toolkit_Download_ShareableImage1:
                        case UserEventEnums.Name.Toolkit_Download_ShareableImage2:
                        case UserEventEnums.Name.Toolkit_Download_ShareableImage3:
                        case UserEventEnums.Name.Toolkit_Download_ShareableImage4:
                            this.SetCustomerActivationStatus(Int32.Parse(eid), EngagementUpdateField.CustomerActivationSharableImages);
                            break;
                        case UserEventEnums.Name.Share_Toolkit:
                            this.SetCustomerActivationStatus(Int32.Parse(eid), EngagementUpdateField.CustomerActivationShareToolkit);
                            break;
                        default:
                            break;
                    }
                }

                result.IsError = false;
                result.ErrorStr = "";
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
        public SetStatusResult SetStatus(string clientId, string engagementId, string name, string value)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("SetStatus");
            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = Int32.Parse(clientId);
                if (gptwContext.EngagementId <= 0)
                    gptwContext.EngagementId = Int32.Parse(engagementId);
            }
            catch (Exception) { }

            SetStatusResult result = new SetStatusResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred."
            };

            try
            {
                if (!IsUserAuthorized(gptwContext, int.Parse(clientId)))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                AtlasCallSetStatusResult setresult = CallAtlasToUpdateListCert(clientId, engagementId, name, value, gptwContext).Result;
                result.IsError = setresult.IsError;
                result.ErrorStr = setresult.ErrorStr;

                if (!result.IsError)
                {
                    if (name.ToLower() == "cstatus" && value.ToLower() == "not certified")
                    {
                        ECRV2 writeableECRV2 = this.AORepository.RetrieveReadWriteECRV2(Int32.Parse(engagementId));
                        if (writeableECRV2 == null)
                            AtlasLog.LogErrorStringOnly(String.Format("RetrieveReadWriteECRV2 failed."), gptwContext);
                        else
                        {
                            writeableECRV2.TakeNotCertifedActions();
                            this.AORepository.SaveECRV2(ref writeableECRV2);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorStr = "SetStatus failed with an unhandled error of: " + e.Message;
            }

            return result;
        }

        [HttpGet("[action]")]
        public OptOutAbandonSurveyResult OptOutAbandonSurvey(string clientId, string engagementId, string surveyType, string actionToTake)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("OptOutAbandonSurvey");
            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = Int32.Parse(clientId);
                if (gptwContext.EngagementId <= 0)
                    gptwContext.EngagementId = Int32.Parse(engagementId);
            }
            catch (Exception) { }

            OptOutAbandonSurveyResult result = new OptOutAbandonSurveyResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred."
            };

            try
            {
                // Check if authorized
                if (!IsUserAuthorized(gptwContext, int.Parse(clientId)))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                // Validate that an allowed actionSelected value was passed
                // Translate the desired action into a new status
                string newStatus = "";
                switch (actionToTake.ToLower().Trim())
                {
                    case "opt out":
                        newStatus = "Opted-Out";
                        break;
                    case "abandon":
                        newStatus = "Abandoned";
                        break;
                    default:
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        result.ErrorStr = "Invalid action parameter: '" + actionToTake + "'";
                        result.ErrorStr = String.Format("Invalid action parameter value: '{0}'. Allowed values are 'Opt Out' and 'Abandon'", actionToTake);
                        return result;
                }

                // Get a read-only copy of the ECR for validation
                int eid = Int32.Parse(engagementId);
                ECRV2 ecrv2 = this.AORepository.RetrieveReadOnlyECRV2(eid);
                if (ecrv2 is null)
                {
                    AtlasLog.LogError(String.Format("Unable to retrieve a read-only ECR for engagementId: {0}", eid), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    result.ErrorStr = String.Format("Unable to retrieve a read-only ECR for engagementId: {0}'", eid);
                    return result;
                }

                surveyType = surveyType.ToUpper().Trim();

                // Except in the case of a CA, validate that the ECR is not already pending or certified
                if (surveyType != "CA")
                {
                    CountryData cd = this.AORepository.FindCertificationCountry(ecrv2);
                    if (cd.CertificationStatus.ToLower() == "certified" || cd.CertificationStatus.ToLower() == "pending")
                    {
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        result.ErrorStr = String.Format("Cannot {0} of the {1} for engagementId {2}. The engagement is already certified or pending", actionToTake, surveyType, eid);
                        return result;
                    }
                }

                // Validate that the current status allows this change
                string currentStatus = "";
                bool statusChangeOk = false;

                switch (surveyType)
                {
                    case "TI":
                        currentStatus = ecrv2.ECR.TrustIndexStatus;
                        if (currentStatus.ToLower() == "created" || currentStatus.ToLower() == "setup in progress")
                        {
                            statusChangeOk = true;
                        }
                        break;
                    case "CB":
                        currentStatus = ecrv2.ECR.CultureBriefStatus;
                        if (currentStatus.ToLower() == "created" || currentStatus.ToLower() == "in progress")
                        {
                            statusChangeOk = true;
                        }
                        break;
                    case "CA":
                        currentStatus = ecrv2.ECR.CultureAuditStatus;
                        if (currentStatus.ToLower() == "created" || currentStatus.ToLower() == "in progress")
                        {
                            statusChangeOk = true;
                        }
                        break;
                    default:
                        Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        result.ErrorStr = String.Format("Invalid surveytype parameter value: '{0}'. Allowed values are 'TI', 'CB', and 'CA'", surveyType);
                        return result;
                }

                if (!statusChangeOk)
                {
                    // The specified survey type was not in a status that allows opting out or abandoning
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    result.ErrorStr = String.Format("Cannot {0} the {1} for engagementId {2}, the current status is {3}", actionToTake, surveyType, eid, currentStatus);
                    this.AORepository.ManuallyClearWriteLock(ecrv2);
                    return result;
                }

                // Get current statuses
                string TIStatus = ecrv2.ECR.TrustIndexStatus.ToLower().Trim();
                string CBStatus = ecrv2.ECR.CultureBriefStatus.ToLower().Trim();
                string CAStatus = ecrv2.ECR.CultureAuditStatus.ToLower().Trim();

                List<EngagementActionResultUpdate> updates = new List<EngagementActionResultUpdate>();

                // Set the new status
                EngagementActionResultUpdate fieldUpdate = new EngagementActionResultUpdate();
                fieldUpdate.NewData = newStatus;
                switch (surveyType)
                {
                    case "TI":
                        fieldUpdate.FieldToUpdate = EngagementUpdateField.TrustIndexStatus;
                        TIStatus = newStatus.ToLower();
                        break;
                    case "CB":
                        fieldUpdate.FieldToUpdate = EngagementUpdateField.CultureBriefStatus;
                        CBStatus = newStatus.ToLower();
                        break;
                    case "CA":
                        fieldUpdate.FieldToUpdate = EngagementUpdateField.CultureAuditStatus;
                        CAStatus = newStatus.ToLower();
                        break;
                    default:
                        break;
                }
                updates.Add(fieldUpdate);

                // Abandon the entire ECR if all included parts (TI, CB, CA) are abandoned
                if (((TIStatus == "abandoned") || (TIStatus == "")) && ((CBStatus == "abandoned") || (CBStatus == "")) && ((CAStatus == "abandoned") || (CAStatus == "")))
                {
                    EngagementActionResultUpdate abandonStatusUpdate = new EngagementActionResultUpdate();
                    abandonStatusUpdate.FieldToUpdate = EngagementUpdateField.IsAbandoned;
                    abandonStatusUpdate.NewData = true.ToString();
                    updates.Add(abandonStatusUpdate);
                }

                // Update the ECR
                UpdateECRV2PropertiesResult updateResult = this.AORepository.UpdateECRV2Properties(eid, updates);

                // EngagementStatus calls
                ECRV2 writeableEcrv2 = this.AORepository.RetrieveReadWriteECRV2(eid);
                if (writeableEcrv2 is null)
                {
                    String errMessage = String.Format("Unable to retrieve a writeableEcrv2 for engagementId: {0}", eid);
                    AtlasLog.LogError(errMessage, gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    result.ErrorStr = errMessage;
                    return result;
                }
                writeableEcrv2.TICACBOptedOutOrAbandoned();
                this.AORepository.SaveECRV2(ref writeableEcrv2);

                // Update the Culture Survey (CB or CA)
                if (surveyType == "CB" || surveyType == "CA")
                {
                    CultureSurveyDTO cs = this.CSRepository.GetCultureSurvey(eid, surveyType);
                    SurveyStatus csStatus = cs.SurveyState;
                    if (newStatus == "Abandoned") csStatus = SurveyStatus.Abandoned;
                    if (newStatus == "Opted-Out") csStatus = SurveyStatus.OptedOut;
                    cs.SurveyState = csStatus;
                    this.CSRepository.SaveCultureSurvey(cs);
                }

                result.IsError = !updateResult.Success;
                result.ErrorStr = updateResult.ErrorMessage;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorStr = "OptOutAbandonSurvey failed with an unhandled error of: " + e.Message;
            }

            return result;
        }

        [HttpGet("[action]")]
        public SetRenewalStatusResult SetRenewalStatus(int clientId, int engagementId, RenewalStatus newRenewalStatus)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("SetRenewalStatus");
            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = clientId;
                if (gptwContext.EngagementId <= 0)
                    gptwContext.EngagementId = engagementId;
            }
            catch (Exception) { }

            SetRenewalStatusResult result = new SetRenewalStatusResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred."
            };

            try
            {
                //RenewalStatus newRenewalStatus = (RenewalStatus)status;
                // Check if authorized
                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    result.IsError = true;
                    result.ErrorStr = String.Format("Not Authorized.token:{0}", authorizationToken);
                    AtlasLog.LogError(result.ErrorStr, gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                ECRV2 writeableEcrv2 = this.AORepository.RetrieveReadWriteECRV2(engagementId);

                if (writeableEcrv2.RenewalStatus != RenewalStatus.ELIGIBLE)
                {
                    result.IsError = true;
                    result.ErrorStr = String.Format("Ecrv2.RenewalStatus != RenewalStatus.ELIGIBLE");
                    AtlasLog.LogError(result.ErrorStr, gptwContext);
                    return result;
                }

                if (newRenewalStatus != RenewalStatus.RENEWED & newRenewalStatus != RenewalStatus.CHURNED)
                {
                    result.IsError = true;
                    result.ErrorStr = String.Format("setting the renewal status to {0} not supported", newRenewalStatus);
                    AtlasLog.LogError(result.ErrorStr, gptwContext);
                    return result;
                }

                writeableEcrv2.RenewalStatus = newRenewalStatus;

                this.AORepository.SaveECRV2(ref writeableEcrv2);

                result.IsError = false;
                result.ErrorStr = "";
                return result;
            }
            catch (Exception e)
            {
                result.IsError = true;
                result.ErrorStr = "Failed with unhandled error of: " + e.Message;
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }

        //[HttpGet("[action]")]
        //public SetCustomerActivationStatusResult SetCustomerActivationStatus(string clientId, string engagementId, string activationField)
        //{
        //    GptwLogContext gptwContext = GetNewGptwLogContext("SetCustomerActivationStatus");
        //    try
        //    {
        //        if (gptwContext.ClientId <= 0)
        //            gptwContext.ClientId = Int32.Parse(clientId);
        //        if (gptwContext.EngagementId <= 0)
        //            gptwContext.EngagementId = Int32.Parse(engagementId);
        //    }
        //    catch (Exception) { }

        //    SetCustomerActivationStatusResult result = new SetCustomerActivationStatusResult
        //    {
        //        IsError = true,
        //        ErrorStr = "A general error has occurred."
        //    };

        //    try
        //    {
        //        // Check if authorized
        //        if (!IsUserAuthorized(gptwContext, int.Parse(clientId)))
        //        {
        //            AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
        //            Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        //            return result;
        //        }

        //        // Get a read-only copy of the ECR to check if an update is needed
        //        int eid = Int32.Parse(engagementId);
        //        ECRV2 ecrv2 = this.AORepository.RetrieveReadOnlyECRV2(eid);
        //        if (ecrv2 is null)
        //        {
        //            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        //            result.ErrorStr = String.Format("Unable to retrieve a read-only ECR for engagementId: {0}'", eid);
        //            AtlasLog.LogError(result.ErrorStr, gptwContext);
        //            return result;
        //        }

        //        EngagementUpdateField updateField = EngagementUpdateField.None;
        //        ActivationStatus currentStatus = ActivationStatus.NA;
        //        switch (activationField.ToLower())
        //        {
        //            case "emprisingaccess":
        //                updateField = EngagementUpdateField.CustomerActivationEmprisingAccess;
        //                currentStatus = ecrv2.CustomerActivationEmprisingAccess;
        //                break;
        //            case "downloadbadge":
        //                updateField = EngagementUpdateField.CustomerActivationBadgeDownload;
        //                currentStatus = ecrv2.CustomerActivationEmprisingAccess;
        //                break;
        //            case "sharetoolkit":
        //                updateField = EngagementUpdateField.CustomerActivationShareToolkit;
        //                currentStatus = ecrv2.CustomerActivationEmprisingAccess;
        //                break;
        //            case "shareableimages":
        //                updateField = EngagementUpdateField.CustomerActivationSharableImages;
        //                currentStatus = ecrv2.CustomerActivationEmprisingAccess;
        //                break;
        //            case "reportdownload":
        //                updateField = EngagementUpdateField.CustomerActivationReportDownload;
        //                currentStatus = ecrv2.CustomerActivationEmprisingAccess;
        //                break;
        //            default:
        //                break;
        //        }

        //        if (updateField == EngagementUpdateField.None)
        //        {
        //            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        //            result.ErrorStr = String.Format("Unsupported field to update '{0}', engagementId '{1}'", activationField, gptwContext.EngagementId);
        //            return result;
        //        }

        //        // Only change the status if it isn't already set
        //        if (currentStatus != ActivationStatus.YES)
        //        {
        //            bool success = this.SetCustomerActivationStatus(gptwContext.EngagementId, updateField);
        //            if (success)
        //            {
        //                result.IsError = false;
        //                result.ErrorStr = "";
        //            }
        //            else
        //            {
        //                result.ErrorStr = String.Format("Error when setting the customer activation status for field '{0}', engagementId '{1}'", updateField.ToString(), gptwContext.EngagementId);
        //                AtlasLog.LogError(String.Format("ERROR:{0}", result.ErrorStr), gptwContext);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
        //        result.ErrorStr = "SetCustomerActivationStatus failed with an unhandled error of: " + e.Message;
        //    }

        //    return result;
        //}

        [HttpGet("[action]")]
        public GeneralCallResult PreValidateNewECRCreation(int clientId, string affiliateId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("PreValidateNewECRCreation");
            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = clientId;
            }
            catch (Exception) { }

            GeneralCallResult result = new GeneralCallResult
            {
                Success = false,
                ErrorMessage = "A general error has occurred."
            };

            try
            {
                // Check if authorized
                if (!IsAuthorizedEmployee(gptwContext, affiliateId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                //// skip validation for MNC CID's
                //if (AORepository.IsCIDAnMNC(clientId))
                //{
                //    result.ErrorMessage = "";
                //    result.Success = true;
                //    return result;
                //}

                // call validation code: IsClientIdReadyForNewECR
                GeneralCallResult generalCallResult = this.IsClientIdReadyForNewECR(clientId, gptwContext);

                result.ErrorMessage = generalCallResult.ErrorMessage;
                result.Success = generalCallResult.Success;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorMessage = "Failed with an unhandled error of: " + e.Message;
            }

            return result;
        }

        [HttpGet("[action]")]
        public GetNewECRCountriesResult GetNewECRCountries(int clientId, string affiliateId, string selectedProduct)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetNewECRCountries");
            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = clientId;
            }
            catch (Exception) { }

            GetNewECRCountriesResult result = new GetNewECRCountriesResult
            {
                Success = false,
                ErrorMessage = "A general error has occurred.",
                Countries = new List<Country>()
            };

            try
            {
                // Check if authorized
                if (!IsAuthorizedEmployee(gptwContext, affiliateId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                if (AORepository.IsCIDAnMNC(clientId) && selectedProduct.ToLower() == "unlimited" && affiliateId == "US1")
                {
                    result.Countries = this.AORepository.GetAllCountries();
                }
                else
                {
                    List<Country> allCountries = this.AORepository.GetAllCountries();

                    Affiliate myAffiliate = this.AORepository.GetAffiliatebyAffiliateId(affiliateId);

                    result.Countries = (from myCountry in allCountries
                                        where myAffiliate.AllowableCountryCodes.Contains(myCountry.CountryCode)
                                        orderby myCountry.CountryName
                                        select myCountry).ToList();
                }

                result.ErrorMessage = "";
                result.Success = true;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorMessage = "Failed with an unhandled error of: " + e.Message;
            }

            return result;
        }

        // Called from InProgress page
        [HttpGet("[action]")]
        public GetListDeadlineInfoResult GetListDeadlineInfo()
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetListDeadlineInfo");

            GetListDeadlineInfoResult returnedResult = new GetListDeadlineInfoResult
            {
                ErrorOccurred = true,
                ErrorMessage = "A general error has occurred.",
                ListDeadlineInfo = new ListDeadlineInfo()
            };

            try
            {

                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return returnedResult;
                }

                // No auth check should be neccessary because were returning general list data. Nothing that is specific to the clientId

                HttpResponseMessage result;

                using (var hc = clientFactory.CreateClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                    string url = appSettings.BusinessDataServicesURL + "/api/clientportal/getNextListCertification";

                    GetNextListCerfificationRequest pst = new GetNextListCerfificationRequest
                    {
                        token = appSettings.BusinessDataServicesToken
                    };

                    result = hc.PostAsync(url, new StringContent(JsonConvert.SerializeObject(pst), UnicodeEncoding.UTF8, "application/json")).Result;
                }

                if (result.IsSuccessStatusCode)
                {
                    string apiResponse = result.Content.ReadAsStringAsync().Result;
                    var records = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(apiResponse);
                    if (records.Count == 1)
                    {
                        string str;
                        if (records[0].TryGetValue("name", out str))
                            returnedResult.ListDeadlineInfo.listName = str;
                        if (records[0].TryGetValue("certified_by", out str))
                        {
                            DateTime thisDate = DateTime.Parse(str);
                            returnedResult.ListDeadlineInfo.listDeadlineDate = thisDate.ToShortDateString();
                        }
                        returnedResult.ErrorMessage = "";
                        returnedResult.ErrorOccurred = false;
                        AtlasLog.LogInformation("SUCCESS", gptwContext);
                    }

                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return returnedResult;
        }


        // Called from NewECR page (employees only) to return CompanyName given a clientId
        [HttpGet("[action]")]
        public GetCompanyNameResult GetCompanyName(int clientId, string affiliateId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetCompanyName");
            gptwContext.ClientId = clientId;

            GetCompanyNameResult result = new GetCompanyNameResult
            {
                ErrorOccurred = true,
                ErrorMessage = "A general error has occurred.",
                CompanyName = ""
            };

            try
            {
                if (!IsAuthorizedEmployee(gptwContext, affiliateId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                AtlasLog.LogInformation("Calling from Portal to Atlas:RetrieveFromBDS", gptwContext);

                List<ClientRecordDetail> clientRecords = RetrieveFromBDS(clientId, gptwContext);

                AtlasLog.LogInformation("Returned from Atlas:RetrieveFromBDS call", gptwContext);

                if (clientRecords.Count == 0)
                {
                    result.ErrorMessage = "0 records were returned";
                    AtlasLog.LogError("ERROR:0 records were returned", gptwContext);
                    return result;
                }

                ClientRecordDetail mostRecent = (from ClientRecordDetail record in clientRecords orderby record.create_dt descending select record).FirstOrDefault();
                result.CompanyName = mostRecent.client_long_name;
                result.ErrorMessage = "";
                result.ErrorOccurred = false;
                AtlasLog.LogInformation("SUCCESS", gptwContext);
                return result;

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }

        // Called from the dashboard when a user clicks a favorite
        [HttpGet("[action]")]
        public FavoriteClickedResult FavoriteClicked(int clientId, string email)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("FavoriteClicked");
            gptwContext.ClientId = clientId;

            FavoriteClickedResult result = new FavoriteClickedResult
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

                this.AORepository.ToggleFavoriteClient(email, clientId);

                result.IsError = false;
                result.ErrorStr = "";
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }


        //[HttpGet("[action]")]
        //public SetPortalContactLoginDateResult SetPortalContactLoginDate(int clientId, string email)
        //{
        //    GptwLogContext gptwContext = GetNewGptwLogContext("SetPortalContactLoginDate");
        //    try
        //    {
        //        if (gptwContext.ClientId <= 0)
        //            gptwContext.ClientId = clientId;
        //    }
        //    catch (Exception) { }

        //    SetPortalContactLoginDateResult result = new SetPortalContactLoginDateResult
        //    {
        //        IsError = true,
        //        ErrorStr = "A general error has occurred."
        //    };

        //    try
        //    {
        //        if (!IsUserAuthorized(gptwContext, clientId))
        //        {
        //            AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
        //            Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        //            return result;
        //        }

        //        result = CallAtlasToSetContactLoginDate(clientId, email, gptwContext).Result;

        //        if (result.IsError)
        //        {
        //            AtlasLog.LogError(String.Format("ERROR:{0}", result.ErrorStr), gptwContext);
        //        }
        //        else
        //        {
        //            AtlasLog.LogInformation("SUCCESS", gptwContext);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
        //    }

        //    return result;
        //}

        // Called from NewECR page (employee only)
        [HttpPost("[action]")]
        public CreateECRResult CreateECR([FromBody] CreateECRRequest createECRRequest)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("CreateECR");
            gptwContext.ClientId = createECRRequest.ClientId;

            CreateECRResult result = new CreateECRResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                ErrorId = ERROR_ID_NOT_DEFINED,
                WarningStr = "",
                EngagementId = -1
            };

            //public class CreateECRRequest
            //{
            //    public string AffiliateId;
            //    public string Username;
            //    public string Password;
            //    public int ClientId;
            //    public string ClientName;
            //    public string TrustIndexSurveyType;
            //    public string CountryCode;
            //    public int TotalEmployees;
            //    public string Email;
            //    public string FirstName;
            //    public string LastName;
            //    public string SessionId;
            //    public string CallerEmail;
            //}

            try
            {
                if (!IsAuthorizedEmployee(gptwContext, createECRRequest.AffiliateId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                // Check that createECRRequest.AffiliateId is allowed to create ecr's for createECRRequest.CountryCode 
                Affiliate affiliateCreatingECR = this.AORepository.GetAffiliatebyAffiliateId(createECRRequest.AffiliateId);

                Boolean isMNCCID = this.AORepository.IsCIDAnMNC(createECRRequest.ClientId);

                if (!isMNCCID)
                {
                    if (!affiliateCreatingECR.AllowableCountryCodes.Contains(createECRRequest.CountryCode))
                    {
                        AtlasLog.LogError(String.Format("Employee Not Authorized to create ECR for CountryCode:{0} .token:{1}", createECRRequest.CountryCode, authorizationToken), gptwContext);
                        Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return result;
                    }
                }

                // Check that the createECRRequest.ClientId is in the allowable range of CID's for the createECRRequest.AffiliateId 
                if (createECRRequest.ClientId < affiliateCreatingECR.StartClientId || createECRRequest.ClientId > affiliateCreatingECR.EndClientId)
                {
                    AtlasLog.LogError(String.Format("Employee from affiliateId:{0} attempting to create ECR using a cid:{1} outside the allowable range. token:{2}", createECRRequest.AffiliateId, createECRRequest.ClientId, authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return result;
                }

                AtlasLog.LogInformation("Calling Atlas:CreateECR WebAPI", gptwContext);

                createECRRequest.SessionId = gptwContext.SessionId;
                createECRRequest.CallerEmail = gptwContext.Email;

                result = CallAtlasToCreateECR(createECRRequest, gptwContext).Result;

                AtlasLog.LogInformation("Returned from call to Atlas:CreateECR WebAPI", gptwContext);

                if (result.IsError)
                {
                    AtlasLog.LogError(String.Format("ERROR:{0}", result.ErrorStr), gptwContext);
                }
                else
                {
                    AtlasLog.LogInformation("SUCCESS", gptwContext);

                    if (createECRRequest.AffiliateId != "US1")
                        AddClient(gptwContext, createECRRequest);

                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }

        [HttpGet("[action]")]
        public ListCalendarResult GetListCalendar()
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetListCalendar");

            ListCalendarResult result = new ListCalendarResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                ListCalendar = new List<ListCalendar>()
            };

            // Establish an operation context and associated telemetry item:
            using (var operation = appInsights.StartOperation<RequestTelemetry>("OperationName-GetListCalendar"))
            {
                appInsights.TrackTrace("PortalControllerEvent:" + gptwContext.MethodName,
                                SeverityLevel.Information,
                                new Dictionary<string, string> { { "cid", gptwContext.ClientId.ToString() }
                            ,{ "eid", gptwContext.EngagementId.ToString()}
                            ,{ "sid", gptwContext.SessionId }
                            ,{ "email", gptwContext.Email }});

                try
                {
                    if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                    {
                        AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                        Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return result;
                    }

                    HttpResponseMessage postResult;

                    using (var hc = clientFactory.CreateClient())
                    {
                        hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                        string url = appSettings.BusinessDataServicesURL + "/api/clientPortal/getListCalendar";

                        var pst = new { token = appSettings.BusinessDataServicesToken };

                        String content = JsonConvert.SerializeObject(pst);

                        StringContent StuffToPost = new StringContent(content);
                        StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                        postResult = hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json")).Result;
                    }

                    if (postResult.IsSuccessStatusCode)
                    {
                        string apiResponse = postResult.Content.ReadAsStringAsync().Result;
                        var records = JsonConvert.DeserializeObject<List<ListCalendar>>(apiResponse);
                        result.IsError = false;
                        result.ErrorStr = "";

                        for (int x = 0; x < records.Count; x++)
                        {
                            ListCalendar thisrecord = records[x];
                            thisrecord.Name = thisrecord.Name.Replace("&trade;", "™");
                            result.ListCalendar.Add(thisrecord);
                        }

                        AtlasLog.LogInformation("SUCCESS", gptwContext);
                    }
                    else
                    {
                        result.IsError = true;
                        result.ErrorStr = "Error-http status " + postResult.StatusCode;
                        AtlasLog.LogError(String.Format("ERROR- {0}", result.ErrorStr), gptwContext);
                        return result;
                    }

                }
                catch (Exception e)
                {
                    AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                    result.IsError = true;
                    result.ErrorStr = "Unhandled exception:" + e.Message;
                }
            }

            return result;

        }

        [HttpGet("[action]")]
        public ListCalendarResult getNextListCertification()
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("getNextListCertification");

            ListCalendarResult result = new ListCalendarResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                ListCalendar = new List<ListCalendar>()
            };

            try
            {
                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                HttpResponseMessage postResult;

                using (var hc = clientFactory.CreateClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                    string url = appSettings.BusinessDataServicesURL + "/api/clientPortal/getNextListCertification";

                    var pst = new { token = appSettings.BusinessDataServicesToken };

                    String content = JsonConvert.SerializeObject(pst);

                    StringContent StuffToPost = new StringContent(content);
                    StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    postResult = hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json")).Result;
                }

                if (postResult.IsSuccessStatusCode)
                {
                    string apiResponse = postResult.Content.ReadAsStringAsync().Result;
                    var records = JsonConvert.DeserializeObject<List<ListCalendar>>(apiResponse);
                    result.IsError = false;
                    result.ErrorStr = "";

                    for (int x = 0; x < records.Count; x++)
                    {
                        ListCalendar thisrecord = records[x];
                        thisrecord.Name = thisrecord.Name.Replace("&trade;", "™");
                        result.ListCalendar.Add(thisrecord);
                    }

                    AtlasLog.LogInformation("SUCCESS", gptwContext);
                }
                else
                {
                    result.IsError = true;
                    result.ErrorStr = "Error-http status " + postResult.StatusCode;
                    AtlasLog.LogError(String.Format("ERROR- {0}", result.ErrorStr), gptwContext);
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
        public GetClientImageInfoResult GetClientImageInfo(int clientId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetClientImageInfo");
            gptwContext.ClientId = clientId;

            GetClientImageInfoResult result = new GetClientImageInfoResult
            {
                ErrorOccurred = true,
                ErrorMessage = "A general error has occurred."
            };

            try
            {
                if (!IsAuthorizedEmployee(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                int engagementId = -1;

                if (!FindEligibleCSToImageEdit(clientId, out engagementId, gptwContext))
                {
                    AtlasLog.LogError(String.Format("ERROR:FindElibleCSToImageEdit failed."), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    result.ErrorMessage = "An error occurred while attempting to find an Eligible ECR, with a CultureBriefStatus of 'Completed' and a ReviewPublishStatus of 'success'.";
                    return result;
                }

                //Make sure that an eid was found
                if (engagementId == -1)
                {
                    AtlasLog.LogError(String.Format("ERROR: An Eligible ECR, with a CultureBriefStatus of 'Completed' and a ReviewPublishStatus of 'success', wasn't found."), gptwContext);
                    result.ErrorMessage = "An Eligible ECR, with a CultureBriefStatus of 'Completed' and a ReviewPublishStatus of 'success', wasn't found.";
                    return result;
                }
                if (engagementId == -2)
                {
                    // Current profile is still in a requested state
                    AtlasLog.LogError(String.Format("ERROR: The latest profile is in a 'Requested' state."), gptwContext);
                    result.ErrorMessage = "The latest profile is in a 'Requested' state. It should only be in 'Requested' state for 30 minutes or so after which you can make your photo edits.";
                    return result;
                }
                if (engagementId == -3)
                {
                    // Current profile is still in a requested state
                    AtlasLog.LogError(String.Format("ERROR: The latest profile is in a 'Failed' state."), gptwContext);
                    result.ErrorMessage = "The latest profile is in a 'Failed' state. This issue must be corrected before you can make any photo edits.";
                    return result;
                }

                var csRepo = new CultureSurveyDataRepository(appSettings.MongoDBConnectionString, appSettings.MongoLockAzureStorageConnectionString);

                CultureSurveyDTO cs = csRepo.GetCultureSurvey(engagementId, "CB");

                if (cs == null)
                {
                    AtlasLog.LogError(String.Format("ERROR: failed to find cb."), gptwContext);
                    result.ErrorMessage = String.Format("The Culture Brief that was selected (associated with eid:{0}) wasn't found within the Atlas Mongo CultureSurvey database. It could be that the CB wasn't completed in the Atlas CultureSurvey system and will need to be edited where it was orginially created.", engagementId);
                    return result;
                }

                //Make sure that CultureSurvey is Complete
                if (cs.SurveyState != SurveyStatus.Complete)
                {
                    AtlasLog.LogError(String.Format("ERROR: Survey wasn't complete."), gptwContext);
                    result.ErrorMessage = "The culturesurvey choosen for editing was not in 'Submitted' status.";
                    return result;
                }

                result.SurveyId = cs.Id.ToString();
                result.SurveyStatus = "COMPLETED";
                result.EngagementId = engagementId;

                ClientImagesDataRepository ciRepo = new ClientImagesDataRepository(appSettings.MongoDBConnectionString);
                ClientImages clientImages = ciRepo.GetClientImages(clientId, engagementId);

                if (clientImages != null)
                {
                    result.ClientPhotos = clientImages.Photos;
                    result.LogoFileName = clientImages.LogoFileName;
                    result.ErrorOccurred = false;
                    result.ErrorMessage = "";
                    AtlasLog.LogInformation("SUCCESS", gptwContext);
                }

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }

        [HttpPost("[action]")]
        public SaveClientImagesResult SaveClientImages([FromBody] SaveClientImagesRequest saveClientImagesRequest)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("SaveClientImages");

            SaveClientImagesResult saveClientImagesResult = new SaveClientImagesResult() { ErrorMessage = "A general error occurred", ErrorOccurred = true };

            try
            {
                // Check twice. Once upfront w/o cid and once later when the cid is available
                if (!IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return saveClientImagesResult;
                }

                var csRepo = new CultureSurveyDataRepository(appSettings.MongoDBConnectionString, appSettings.MongoLockAzureStorageConnectionString);

                CultureSurveyDTO cs = csRepo.GetCultureSurvey(ObjectId.Parse(saveClientImagesRequest.CultureSurveyId));

                if (!IsAuthorizedEmployee(gptwContext, cs.ClientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return saveClientImagesResult;
                }

                ClientImagesDataRepository repo = new ClientImagesDataRepository(appSettings.MongoDBConnectionString);

                ClientImages images = new ClientImages();

                images.ClientId = cs.ClientId;
                images.EngagementId = cs.EngagementId;
                images.LogoFileName = saveClientImagesRequest.LogoFileName;
                images.Photos = saveClientImagesRequest.clientPhotos;

                repo.SaveClientImages(images);

                saveClientImagesResult.ErrorMessage = "";
                saveClientImagesResult.ErrorOccurred = false;

                AtlasLog.LogInformation("SUCCESS", gptwContext);
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return saveClientImagesResult;
        }

        [HttpGet("[action]")]
        public EcrSearchResult SearchECRs(string value)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("SearchECRs");

            EcrSearchResult dashboardSearchResult = new EcrSearchResult() { ErrorStr = "A general error occurred", IsError = true };

            try
            {
                if (!IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return dashboardSearchResult;
                }

                // Get list of Employee Affiliates
                List<Affiliate> employeeAffiliates = GetEmployeeAffiliates(gptwContext);

                // if number then cid/eid search
                // if string then cname search

                dashboardSearchResult = this.AORepository.SearchECRs(value, employeeAffiliates);

                AtlasLog.LogInformation("SUCCESS", gptwContext);

                dashboardSearchResult.IsError = false;
                dashboardSearchResult.ErrorStr = "";
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                dashboardSearchResult.IsError = true;
                dashboardSearchResult.ErrorStr = "";
            }

            return dashboardSearchResult;
        }

        [HttpPost("[action]")]
        public DeleteClientImageResult DeleteClientImageFromBlobStorage([FromBody] DeleteClientImageRequest deleteClientImageRequest)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("DeleteClientImageFromBlobStorage");

            DeleteClientImageResult deleteClientImageResult = new DeleteClientImageResult() { ErrorMessage = "A general error occurred", ErrorOccurred = true };

            try
            {
                if (!IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return deleteClientImageResult;
                }

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(appSettings.ClientAssetsBlobStorageConnectionString);

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = blobClient.GetContainerReference("images");

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(deleteClientImageRequest.FileName);

                blockBlob.DeleteIfExistsAsync();

                deleteClientImageResult.ErrorMessage = "";
                deleteClientImageResult.ErrorOccurred = false;

                AtlasLog.LogInformation("SUCCESS", gptwContext);
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return deleteClientImageResult;
        }

        [HttpGet("[Action]")]
        public async Task<IActionResult> GetClientAsset(string fileName)
        {

            // Validate that the filename does not contain invalid characters
            if (HasInvalidFileNameChars(fileName))
            {
                AtlasLog.LogError("PortalController.GetClientAsset() failed. Filename parameter contains invalid characters for a filename");
                return new BadRequestResult();
            }

            try
            {
                MemoryStream ms = new MemoryStream();

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(appSettings.ClientAssetsBlobStorageConnectionString);

                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer container = BlobClient.GetContainerReference("images");

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference(fileName);

                    if (await file.ExistsAsync())
                    {
                        await file.DownloadToStreamAsync(ms);
                        Stream blobStream = file.OpenReadAsync().Result;
                        return File(blobStream, file.Properties.ContentType, file.Name);
                    }
                    else
                    {
                        return Content("File does not exist");
                    }
                }
                else
                {
                    return Content("Container does not exist");
                }

            }
            catch (Exception e)
            {
                return Content("GetClientAsset Failed.");
            }
        }

        // Called from the Edit CB Images Page
        [HttpPost("[action]")]
        public RepublishProfileResult RepublishProfile([FromBody] RepublishProfileRequest republishProfileRequest)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("RepublishProfile");
            gptwContext.ClientId = republishProfileRequest.ClientId;
            gptwContext.EngagementId = republishProfileRequest.EngagementId;

            RepublishProfileResult result = new RepublishProfileResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
            };

            try
            {
                if (!IsAuthorizedEmployee(gptwContext, republishProfileRequest.ClientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                result = CallAtlasToRepublishProfile(republishProfileRequest, gptwContext).Result;

                if (result.IsError)
                {
                    AtlasLog.LogError(String.Format("ERROR:{0}", result.ErrorStr), gptwContext);
                }
                else
                {
                    AtlasLog.LogInformation("SUCCESS", gptwContext);
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }

        // Called from the Edit CB Images Page
        [HttpPost("[action]")]
        public RepublishProfileResult GetProfilePublishStatus([FromBody] RepublishProfileRequest getProfilePublishStatus)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetProfilePublishStatus");
            gptwContext.ClientId = getProfilePublishStatus.ClientId;
            gptwContext.EngagementId = getProfilePublishStatus.EngagementId;

            RepublishProfileResult result = new RepublishProfileResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
            };

            try
            {
                if (!IsAuthorizedEmployee(gptwContext, getProfilePublishStatus.ClientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken));
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                ECRV2 ecrv2 = this.AORepository.RetrieveReadOnlyECRV2(getProfilePublishStatus.EngagementId);

                if (ecrv2 != null)
                {
                    result.ReviewPublishStatus = this.AORepository.FindUSCountryData(ecrv2).ProfilePublishStatus;
                    result.IsError = false;
                    result.ErrorStr = "";
                    //AtlasLog.LogInformation("SUCCESS", gptwContext);
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e);
            }

            return result;
        }

        [HttpGet("[action]")]
        public GetClientEmailDataResult GetClientEmailData(int engagementid)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetClientEmailData");

            GetClientEmailDataResult result = new GetClientEmailDataResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                data = new List<ClientEmail>()
            };

            try
            {
                if (!IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                List<EmailTracking> emailTrackingObjects = this.AORepository.GetEmailTrackingByEngagementId(engagementid);

                for (int i = 0; i < emailTrackingObjects.Count; i++)
                {
                    EmailTracking emailTracking = emailTrackingObjects[i];
                    ClientEmail clientEmail = new ClientEmail();
                    clientEmail.Id = emailTracking.Id.ToString();
                    clientEmail.EmailType = ECRV2.GetEnumDescription(emailTracking.EmailType);
                    clientEmail.ClientId = emailTracking.ClientId;
                    clientEmail.EngagementId = emailTracking.EngagementId;
                    clientEmail.DateTimeSent = emailTracking.DateTimeSent;
                    clientEmail.Subject = emailTracking.Subject;
                    clientEmail.Body = emailTracking.Body;
                    clientEmail.Address = emailTracking.Address;
                    clientEmail.Opened = emailTracking.Opened;
                    if (emailTracking.DateTimeOpened.Count > 0)
                    {
                        clientEmail.dateTimeOpened = emailTracking.DateTimeOpened[0];
                        foreach (DateTime date in emailTracking.DateTimeOpened)
                            clientEmail.dateTimeOpenedList.Add(date);
                    }
                    clientEmail.IsError = emailTracking.IsError;
                    clientEmail.ErrorMessage = emailTracking.ErrorMessage;
                    result.data.Add(clientEmail);
                }

                result.IsError = false;
                result.ErrorStr = "";
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
        public ContentResult GetEmail(string id, string token)
        {
            ContentResult result = new ContentResult();
            this.SetTokenFromExternalSource(token);
            GptwLogContext gptwContext = GetNewGptwLogContext("GetHTML", null);
            try
            {
                EmailTracking emailTracking = this.AORepository.GetEmailTracking(id);

                gptwContext.ClientId = emailTracking.ClientId;
                gptwContext.EngagementId = emailTracking.EngagementId;

                if (!IsAuthorizedEmployee(gptwContext, emailTracking.ClientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                result.Content = emailTracking.Body;

                string subject = emailTracking.Subject.Replace("’", "&rsquo;");
                result.Content = result.Content.Replace("<title></title>", "<title>" + subject + "</title>");

                result.ContentType = "text/html";
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }
            // The token is no longer needed to be overidden at this point so clear it.
            this.ClearTokenFromExternalSource(token);
            return result;
        }

        [HttpGet("[action]")]
        public GetDashboardDataResult GetDashboardData(string affiliateId, string email, Boolean showMyFavorites)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetDashboardData");

            GetDashboardDataResult result = new GetDashboardDataResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                ECRV2s = new List<ECRV2Info>()
            };

            try
            {
                if (!IsAuthorizedEmployee(gptwContext, affiliateId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                FavoriteClients favoriteClients = this.AORepository.GetFavoriteClients(email);

                DateTime date2YearsAgo = DateTime.Now.AddDays(-365 * 3);

                List<ECRV2Snippet> ECRV2s = this.AORepository.RetrieveECRV2Snippets(date2YearsAgo, affiliateId);

                for (int i = 0; i < ECRV2s.Count; i++)
                {
                    ECRV2Snippet ecrSnippet = ECRV2s[i];

                    if (ecrSnippet.Abandoned)
                        continue;

                    ECRV2Info info = new ECRV2Info();
                    info.cid = ecrSnippet.ClientId;

                    Boolean isAFavorite = favoriteClients != null && favoriteClients.Clients.Contains(info.cid);

                    if (!isAFavorite && showMyFavorites)
                    {
                        continue;
                    }

                    info.favorite = isAFavorite;

                    info.cname = ecrSnippet.ClientName;
                    info.id = ecrSnippet.Id.ToString();

                    info.eid = ecrSnippet.EngagementId;
                    info.tistatus = ecrSnippet.TrustIndexStatus;
                    info.cbstatus = ecrSnippet.CultureBriefStatus;
                    info.castatus = ecrSnippet.CultureAuditStatus;
                    info.createdate = ecrSnippet.CreatedDate.Year + "-" + ecrSnippet.CreatedDate.Month + "-" + ecrSnippet.CreatedDate.Day;
                    info.cstatus = ecrSnippet.CertificationStatus;
                    info.calink = ecrSnippet.CultureAuditSSOLink;
                    info.cblink = ecrSnippet.CultureBriefSSOLink;
                    info.tilink = ecrSnippet.TrustIndexSSOLink;
                    info.rstatus = ecrSnippet.ProfilePublishStatus;
                    info.rlink = "";
                    if (!String.IsNullOrEmpty(ecrSnippet.ProfilePublishStatus) && ecrSnippet.ProfilePublishStatus.ToLower() == "success")
                        info.rlink = ecrSnippet.ProfilePublishedLink;
                    info.lstatus = ecrSnippet.ListEligibilityStatus;
                    info.certexdate = "";
                    if (ecrSnippet.CertificationExpiryDate != null)
                    {
                        DateTime certificationExpiryDate = (DateTime)ecrSnippet.CertificationExpiryDate;
                        info.certexdate = certificationExpiryDate.Year + "-" + certificationExpiryDate.Month + "-" + certificationExpiryDate.Day;
                    }
                    info.country = ecrSnippet.Country;
                    info.tier = ecrSnippet.Tier;
                    info.journeystatus = ecrSnippet.JourneyStatus;
                    info.journeyhealth = ecrSnippet.JourneyHealth;
                    info.duration = ecrSnippet.Duration;
                    info.renewalstatus = ecrSnippet.RenewalStatus;
                    info.renewalhealth = ecrSnippet.RenewalHealth;
                    info.engagementstatus = ecrSnippet.EngagementStatus;
                    info.engagementhealth = ecrSnippet.EngagementHealth;
                    info.numberofsurveyrespondents = ecrSnippet.NumberOfSurveyRespondents;
                    info.surveyopendate = "";
                    if (ecrSnippet.SurveyOpenDate != null)
                    {
                        DateTime surveyOpenDate = (DateTime)ecrSnippet.SurveyOpenDate;
                        info.surveyopendate = surveyOpenDate.Year + "-" + surveyOpenDate.Month + "-" + surveyOpenDate.Day;
                    }
                    info.surveyclosedate = "";
                    if (ecrSnippet.SurveyCloseDate != null)
                    {
                        DateTime surveyCloseDate = (DateTime)ecrSnippet.SurveyCloseDate;
                        info.surveyclosedate = surveyCloseDate.Year + "-" + surveyCloseDate.Month + "-" + surveyCloseDate.Day;
                    }
                    info.allcountrycertification = ecrSnippet.AllCountryCertification;
                    info.allcountrylisteligiblity = ecrSnippet.AllCountryListEligiblity;
                    result.ECRV2s.Add(info);
                }

                result.IsError = false;
                result.ErrorStr = "";
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
        public bool IsValidEmail(string email)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("IsValidEmail");

            try
            {
                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return false;
                }

                return CommonValidations.IsEmailAddressValid(email);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        [HttpGet("[action]")]
        public IActionResult DownloadDataRequest(string id)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("DownloadDataRequest");
            string requesterEmail = gptwContext.Email;
            // retrieve the file from report store and stream to user

            try
            {

                if (!IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return NotFound();
                }

                // We know that this is an employee/affilaite
                // Now we need to determine if the the user download the data has the affiliate claim associated with the data request affiliate claim

                DataExtractRequestQueue dataRequest = this.AORepository.GetDataExtractRequestQueueById(id);

                if (dataRequest != null)
                {
                    List<Affiliate> employeeAffiliates = GetEmployeeAffiliates(gptwContext);
                    Affiliate affiliate = (from aff in employeeAffiliates.AsQueryable() where (aff.AffiliateId == dataRequest.AffiliateId) select aff).SingleOrDefault();
                    if (affiliate != null)
                    {
                        //var url = "http://localhost:49580/";
                        var url = appSettings.ReportStoreUrl;
                        AtlasDataRequestReportStoreFileStreamer fileStreamer = new AtlasDataRequestReportStoreFileStreamer(url, dataRequest.AffiliateId, "api@greatplacetowork.com", appSettings.ReportStoreServicesToken);
                        var reportFile = fileStreamer.GetStream(new GptwUri() { Uri = dataRequest.ReportLink });

                        return File(reportFile, "application/octet-stream");
                    }
                }
                return NotFound();
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                var reportFile = "Sorry, you cannot download the file. If you continue to receive this message, please contact our support team.";
                return File(reportFile, "application/octet-stream");
            }
        }

        [HttpGet("[action]")]
        public GetCertAPPInfoResult GetCertAPPInfo(int clientId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetCertAPPInfo");

            GetCertAPPInfoResult result = new GetCertAPPInfoResult
            {
                ErrorOccurred = true,
                ErrorMessage = "A general error has occurred."
            };

            try
            {
                if (!IsUserAuthorized(gptwContext, clientId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                List<ECRV2> ecrv2s = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);

                if (ecrv2s != null)
                {
                    List<CertificationApplication> openApps = new();
                    List<CertificationApplication> submittedApps = new();

                    foreach (var ecrv2 in ecrv2s)
                    {
                        CertificationApplication caApp = new();
                        string tiStatus = ecrv2.ECR.TrustIndexStatus;
                        string caStatus = ecrv2.ECR.CultureAuditStatus;
                        string cbStatus = ecrv2.ECR.CultureBriefStatus;

                        CertificationApplication certApp = new()
                        {
                            EngagementId = ecrv2.EngagementId,
                            CBStatus = cbStatus,
                            CAStatus = caStatus,
                            CreateDate = ecrv2.CreatedDate,
                            CreateDateDisplay = ecrv2.CreatedDate.ToString("MM/yyyy"),
                            CountryCount = ecrv2.ECR.Countries == null ? 0 : ecrv2.ECR.Countries.Count,
                            CultureAuditLink = ecrv2.ECR.CultureAuditSSOLink
                        };

                        if (ecrv2.ECR.Countries != null && ecrv2.ECR.Countries.Count > 0)
                        {
                            List<string> countryCodeList = new();
                            foreach (var country in ecrv2.ECR.Countries)
                                countryCodeList.Add(country.CountryCode);

                            if (countryCodeList.Count > 0)
                            {
                                List<Country> countryDataList = this.AORepository.GetCountryByCountryCodeList(countryCodeList);

                                List<string> countryNameList = new();
                                foreach (var ecrCountry in ecrv2.ECR.Countries)
                                {
                                    var country = countryDataList.FirstOrDefault(x => x.CountryCode == ecrCountry.CountryCode);
                                    if (ecrCountry.IsApplyingForCertification == "Yes")
                                        countryNameList.Add(country == null ? string.Empty : country.CountryName);
                                    else
                                        countryNameList.Add((country == null ? string.Empty : country.CountryName) + "*");
                                }

                                if (countryNameList.Count > 0)
                                    certApp.Countries = string.Join(", ", countryNameList.OrderBy(p => p));
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(caStatus) &&
                            !caStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase) &&
                            !caStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase))
                        {
                            caApp = new CertificationApplication
                            {
                                CAStatus = GetTbcCultureAuditStatus(certApp.CAStatus).ToString().Replace("_", " "),
                                CBStatus = certApp.CBStatus,
                                Countries = certApp.Countries,
                                CountryCount = certApp.CountryCount,
                                CreateDate = certApp.CreateDate.AddSeconds(1),
                                CreateDateDisplay = certApp.CreateDateDisplay,
                                EngagementId = certApp.EngagementId,
                                IsCA = true,
                                IsCB = false,
                                CultureAuditLink = certApp.CultureAuditLink
                            };
                        }

                        var tbcStatusSection = GetTbcCultureBriefStatusAndSection(cbStatus, tiStatus);

                        if (tbcStatusSection.Status == TableOfContentStatus.Hidden && tbcStatusSection.Section == TableOfContentSection.Hidden)
                            continue;

                        certApp.CBStatus = tbcStatusSection.Status.ToString().Replace("_", " ");
                        certApp.IsCA = false;
                        certApp.IsCB = true;
                        if (tbcStatusSection.Section == TableOfContentSection.OpenApplication)
                            openApps.Add(certApp);
                        else if (tbcStatusSection.Section == TableOfContentSection.SubmittedApplication)
                            submittedApps.Add(certApp);

                        if (caApp.CAStatus.Equals("not started", StringComparison.OrdinalIgnoreCase) |
                            caApp.CAStatus.Equals("in progress", StringComparison.OrdinalIgnoreCase))
                            openApps.Add(caApp);
                        if (caApp.CAStatus.Equals("complete", StringComparison.OrdinalIgnoreCase))
                            submittedApps.Add(caApp);
                    }

                    result.OpenApplications = openApps.OrderByDescending(p => p.CreateDate).ToList();
                    result.SubmittedApplications = submittedApps.OrderByDescending(p => p.CreateDate).ToList();
                }

                result.ErrorOccurred = false;
                result.ErrorMessage = "";
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorOccurred = true;
                result.ErrorMessage = "Unhandled exception:" + e.Message;
            }

            return result;
        }

        [HttpGet("[action]")]
        public GetDataExtractRequestsResult GetDataExtractRequests()
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetDataExtractRequests");

            GetDataExtractRequestsResult result = new GetDataExtractRequestsResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                DataExtractRequests = new List<DataExtractRequest>()
            };

            try
            {
                if (!IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                var affiliates = GetEmployeeAffiliates(gptwContext);
                List<DataExtractRequestQueue> dataExtractRequestQueues = this.AORepository.GetDataExtractRequestQueueByAffiliates(affiliates.Select(p => p.AffiliateId));

                foreach (var dataExtract in dataExtractRequestQueues)
                {
                    result.DataExtractRequests.Add(new DataExtractRequest
                    {
                        DateRequested = dataExtract.RequestDate,
                        Status = DataRequestEnums.GetEnumDescription(dataExtract.Status),
                        Link = dataExtract.ReportLink,
                        AffiliateId = dataExtract.AffiliateId,
                        Id = dataExtract.Id.ToString(),
                        Requestor = dataExtract.RequestorEmail
                    });
                }

                result.IsError = false;
                result.ErrorStr = "";
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.IsError = true;
                result.ErrorStr = "Unhandled exception:" + e.Message;
            }

            return result;
        }

        [HttpPost("[Action]")]
        public SubmitDataRequestResult SubmitDataRequest(DataRequestInfo dataRequestInfo)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("SubmitDataRequest", null);

            SubmitDataRequestResult result = new SubmitDataRequestResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
            };

            if (!IsAuthorizedEmployee(gptwContext, dataRequestInfo.AffiliateId))
            {
                AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return result;
            }

            try
            {
                DataExtractRequestQueue dataExtractRequestQueue = (new DataExtractRequestQueue
                {
                    RequestorEmail = dataRequestInfo.RequestorEmail,
                    AffiliateId = dataRequestInfo.AffiliateId,
                    UploadedFileName = dataRequestInfo.UploadedFileName
                });

                this.AORepository.SaveDataExtractRequestQueue(ref dataExtractRequestQueue);

                result.IsError = false;
                result.ErrorStr = "";
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.IsError = true;
                result.ErrorStr = "Unhandled exception:" + e.Message;
            }

            return result;
        }

        //[NonAction]
        //private List<DataExtractRequestQueue> FilterDataExtractRequestQueuesByAffiliates(GptwLogContext gptwContext,
        //    List<DataExtractRequestQueue> dataExtractRequestQueues)
        //{
        //    List<DataExtractRequestQueue> result = new List<DataExtractRequestQueue>();

        //    return result;
        //}

        [HttpGet("[action]")]
        public GetDataExtractRequestsResult GetDataExtractRequestByEmail(string email)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetDataExtractRequestByEmail");

            GetDataExtractRequestsResult result = new GetDataExtractRequestsResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                DataExtractRequests = new List<DataExtractRequest>()
            };

            try
            {
                if (!IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                List<DataExtractRequestQueue> dataExtractRequestQueues = this.AORepository.GetDataExtractRequestQueueByEmail(email);

                foreach (var dataExtract in dataExtractRequestQueues)
                {
                    result.DataExtractRequests.Add(new DataExtractRequest
                    {
                        DateRequested = dataExtract.RequestDate,
                        Status = DataRequestEnums.GetEnumDescription(dataExtract.Status),
                        Link = dataExtract.ReportLink
                    });
                }

                result.IsError = false;
                result.ErrorStr = "";
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
        public ReturnCountries GetAllCountries()
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetCountriesForAffiliate");
            ReturnCountries returnCountries = new ReturnCountries { IsSuccess = false, ErrorMessage = "", Countries = new List<Country>() };

            try
            {
                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return returnCountries;
                }

                returnCountries.Countries = this.AORepository.GetAllCountries();
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                returnCountries.ErrorMessage = e.Message;
                returnCountries.IsSuccess = false;
            }
            return returnCountries;
        }

        [HttpPost("[action]")]
        public CreateECRResult CreateECR2([FromBody] CreateEcrRequest2 createEcrRequest)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("CreateECR2");
            gptwContext.ClientId = createEcrRequest.ClientId;

            CreateECRResult result = new CreateECRResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                ErrorId = ERROR_ID_NOT_DEFINED,
                WarningStr = "",
                EngagementId = -1
            };

            try
            {
                if (!IsAuthorizedEmployee(gptwContext, createEcrRequest.AffiliateId))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }

                GeneralCallResult generalCallResult = this.IsClientIdReadyForNewECR2(createEcrRequest.ClientId, createEcrRequest.IsCreateCA,
                    createEcrRequest.SurveyCountries.Select(p => p.CountryCode.Trim().ToUpper()).ToList(), gptwContext);
                if (generalCallResult != null && !generalCallResult.Success)
                {
                    result.ErrorId = 999;
                    result.ErrorStr = generalCallResult.ErrorMessage;
                    return result;
                }

                // Check that createECRRequest.AffiliateId is allowed to create ecr's for createECRRequest.CountryCode 
                Affiliate affiliateCreatingECR = this.AORepository.GetAffiliatebyAffiliateId(createEcrRequest.AffiliateId);

                // Check that the createECRRequest.ClientId is in the allowable range of CID's for the createECRRequest.AffiliateId 
                if (createEcrRequest.ClientId < affiliateCreatingECR.StartClientId || createEcrRequest.ClientId > affiliateCreatingECR.EndClientId)
                {
                    AtlasLog.LogError(String.Format("Employee from affiliateId:{0} attempting to create ECR using a cid:{1} outside the allowable range. token:{2}", createEcrRequest.AffiliateId, createEcrRequest.ClientId, authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return result;
                }

                AtlasLog.LogInformation("Calling Atlas:CreateECR2 WebAPI", gptwContext);

                createEcrRequest.SessionId = gptwContext.SessionId;
                createEcrRequest.CallerEmail = gptwContext.Email;

                result = CallAtlasToCreateEcr2(createEcrRequest, gptwContext).Result;

                AtlasLog.LogInformation("Returned from call to Atlas:CreateECR2 WebAPI", gptwContext);

                if (result.IsError)
                {
                    AtlasLog.LogError(String.Format("ERROR:{0}", result.ErrorStr), gptwContext);
                }
                else
                {
                    AtlasLog.LogInformation("SUCCESS", gptwContext);

                    if (createEcrRequest.AffiliateId != "US1")
                        AddClient2(gptwContext, createEcrRequest);

                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return result;
        }

        [HttpGet("[action]")]
        public GetDataExtractCompanyInfoByClientIdResult GetDataExtractCompanyInfoByClientId(int clientId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetEngagementsByClientId");
            GetDataExtractCompanyInfoByClientIdResult getDataExtractCompanyInfoByClientIdResult = new GetDataExtractCompanyInfoByClientIdResult
            {
                IsSuccess = false,
                ErrorMessage = "",
                DataExtractCompanyInfos = new List<DataExtractCompanyInfo>()
            };

            try
            {
                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return getDataExtractCompanyInfoByClientIdResult;
                }

                List<ECRV2> ecrv2s = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);

                foreach (ECRV2 ecrv2 in ecrv2s)
                {
                    getDataExtractCompanyInfoByClientIdResult.DataExtractCompanyInfos.Add(
                        new DataExtractCompanyInfo
                        {
                            ClientId = ecrv2.ClientId,
                            EngagementId = ecrv2.EngagementId,
                            ClientName = ecrv2.ClientName
                        });
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                getDataExtractCompanyInfoByClientIdResult.ErrorMessage = e.Message;
                getDataExtractCompanyInfoByClientIdResult.IsSuccess = false;
            }
            return getDataExtractCompanyInfoByClientIdResult;
        }

        //[HttpPost("[Action]")]
        //public SubmitDataRequestResult SubmitDataExtractRequest(DataExtractRequestData dataRequestInfo)
        //{
        //    GptwLogContext gptwContext = GetNewGptwLogContext("SubmitDataExtractRequest", null);

        //    SubmitDataRequestResult result = new SubmitDataRequestResult
        //    {
        //        IsError = true,
        //        ErrorStr = "A general error has occurred.",
        //    };

        //    if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
        //    {
        //        AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
        //        Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        //        return result;
        //    }

        //    try
        //    {
        //        DataExtractRequestQueueV2 dataExtractRequestQueue = new DataExtractRequestQueueV2(
        //            new DataRequestInfo
        //            {
        //                RequestorEmail = dataRequestInfo.RequestorEmail,
        //                AffiliateId = "",
        //                UploadedFileName = dataRequestInfo.FileName
        //            },
        //            new DataRequestParameters
        //            {
        //                CountryCode = dataRequestInfo.CountryCode,
        //                TrustIndexData = dataRequestInfo.TrustIndexData,
        //                CultureBriefDatapoints = dataRequestInfo.CultureBriefDatapoints,
        //                CultureAuditEssays = dataRequestInfo.CultureAuditEssays,
        //                PhotosAndCaptions = dataRequestInfo.PhotosAndCaptions,
        //                CertificationExpiry = dataRequestInfo.CertificationExpiry,
        //                CompletedCultureAudit = dataRequestInfo.CompletedCultureAudit,
        //                Industry = dataRequestInfo.Industry,
        //                IndustryVertical = dataRequestInfo.IndustryVertical,
        //                Industry2 = dataRequestInfo.Industry2,
        //                IndustryVertical2 = dataRequestInfo.IndustryVertical2,
        //                MinimumNumberEmployees = dataRequestInfo.MinimumNumberEmployees,
        //                MaximumNumberEmployees = dataRequestInfo.MaximumNumberEmployees,
        //                DataRequestCompanies = new List<DataRequestCompany>()
        //            });

        //        if (dataRequestInfo.DataExtractCompanyInfos != null && dataRequestInfo.DataExtractCompanyInfos.Length > 0)
        //        {
        //            dataExtractRequestQueue.DataRequestParameters.DataRequestCompanies = dataRequestInfo.DataExtractCompanyInfos.Select(p =>
        //                new DataRequestCompany()
        //                {
        //                    ClientId = p.ClientId,
        //                    EngagementId = p.EngagementId,
        //                    CompanyName = p.ClientName
        //                }).ToList();
        //        }

        //        this.AORepository.SaveDataExtractRequest(ref dataExtractRequestQueue);

        //        result.IsError = false;
        //        result.ErrorStr = "";
        //    }
        //    catch (Exception e)
        //    {
        //        AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
        //        result.IsError = true;
        //        result.ErrorStr = "Unhandled exception:" + e.Message;
        //    }

        //    return result;
        //}

        //[HttpGet("[action]")]
        //public GetDataExtractRequestResult GetDataExtractRequestById(string id)
        //{
        //    GptwLogContext gptwContext = GetNewGptwLogContext("GetEngagementsByClientId");
        //    GetDataExtractRequestResult getDataExtractRequestResult = new GetDataExtractRequestResult
        //    {
        //        IsSuccess = false,
        //        ErrorMessage = ""
        //    };

        //    try
        //    {
        //        if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
        //        {
        //            AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
        //            Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        //            return getDataExtractRequestResult;
        //        }

        //        DataExtractRequestQueueV2 dataExtractRequestQueueV2 = this.AORepository.GetDataExtractRequestQueueV2ById(id);

        //        if (dataExtractRequestQueueV2 != null)
        //        {
        //            getDataExtractRequestResult.IsSuccess = true;
        //            getDataExtractRequestResult.FileName = dataExtractRequestQueueV2.UploadedFileName;
        //            getDataExtractRequestResult.RequestorEmail = dataExtractRequestQueueV2.RequestorEmail;
        //            getDataExtractRequestResult.Status = DataRequestEnums.GetEnumDescription(dataExtractRequestQueueV2.Status);
        //            getDataExtractRequestResult.ReportLink = dataExtractRequestQueueV2.ReportLink;

        //            getDataExtractRequestResult.CountryCode = dataExtractRequestQueueV2.DataRequestParameters.CountryCode;
        //            getDataExtractRequestResult.TrustIndexData = dataExtractRequestQueueV2.DataRequestParameters.TrustIndexData;
        //            getDataExtractRequestResult.CultureBriefDatapoints = dataExtractRequestQueueV2.DataRequestParameters.CultureBriefDatapoints;
        //            getDataExtractRequestResult.CultureAuditEssays = dataExtractRequestQueueV2.DataRequestParameters.CultureAuditEssays;
        //            getDataExtractRequestResult.PhotosAndCaptions = dataExtractRequestQueueV2.DataRequestParameters.PhotosAndCaptions;

        //            if (dataExtractRequestQueueV2.DataRequestParameters.DataRequestCompanies != null &&
        //                dataExtractRequestQueueV2.DataRequestParameters.DataRequestCompanies.Count > 0)
        //            {
        //                getDataExtractRequestResult.HowToSelectCompanies = "List of Companies";
        //                getDataExtractRequestResult.DataExtractCompanyInfos = dataExtractRequestQueueV2.DataRequestParameters.DataRequestCompanies.Select(p => new DataExtractCompanyInfo
        //                {
        //                    ClientId = p.ClientId,
        //                    ClientName = p.CompanyName,
        //                    EngagementId = p.EngagementId
        //                }).ToArray();
        //            }
        //            else
        //            {
        //                getDataExtractRequestResult.HowToSelectCompanies = "Certification Date";
        //                getDataExtractRequestResult.CertificationExpiry = dataExtractRequestQueueV2.DataRequestParameters.CertificationExpiry;
        //                getDataExtractRequestResult.CompletedCultureAudit = dataExtractRequestQueueV2.DataRequestParameters.CompletedCultureAudit;
        //                getDataExtractRequestResult.Industry = dataExtractRequestQueueV2.DataRequestParameters.Industry;
        //                getDataExtractRequestResult.IndustryVertical = dataExtractRequestQueueV2.DataRequestParameters.IndustryVertical;
        //                getDataExtractRequestResult.Industry2 = dataExtractRequestQueueV2.DataRequestParameters.Industry2;
        //                getDataExtractRequestResult.IndustryVertical2 = dataExtractRequestQueueV2.DataRequestParameters.IndustryVertical2;
        //                getDataExtractRequestResult.MinimumNumberEmployees = dataExtractRequestQueueV2.DataRequestParameters.MinimumNumberEmployees;
        //                getDataExtractRequestResult.MaximumNumberEmployees = dataExtractRequestQueueV2.DataRequestParameters.MaximumNumberEmployees;
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
        //        getDataExtractRequestResult.ErrorMessage = e.Message;
        //        getDataExtractRequestResult.IsSuccess = false;
        //    }
        //    return getDataExtractRequestResult;
        //}

        // NON ACTION Methods generatlly don't need a GptwLogContext gptwContext = GetNewGptwLogContext();
        // ONLY PUBLIC WebAPI methods. If you want to log inside a non action then the best thing to do is to pass
        // down the parent GptwContext and use that context to log. That way ALL the work that is done within a controller
        // call gets logged to the same Atlas thread with the same sessionid which makes it easy to connect the dots when throubleshooting

        [NonAction]
        // NOTE GetClaims must have been called for this to work
        private List<Affiliate> GetEmployeeAffiliates(GptwLogContext gptwContext)
        {
            List<Affiliate> allAffiliates = this.AORepository.GetAllAffiliates();
            List<Affiliate> employeeAffiliates = new List<Affiliate>();
            foreach (Affiliate affiliate in allAffiliates)
            {
                ClaimResult claimResult = GetClaim(gptwContext, string.Format("GptwAd_GptwAffiliateId_{0}", affiliate.AffiliateId));

                if (claimResult.isSuccess)
                    employeeAffiliates.Add(affiliate);
            }
            return employeeAffiliates;
        }

        [NonAction]
        private async Task<AtlasCallSetStatusResult> CallAtlasToUpdateListCert(string clientId, string engagementId, string name, string value, GptwLogContext parentGptwContext)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("CallAtlasSetStatus", parentGptwContext);

            AtlasCallSetStatusResult result = new AtlasCallSetStatusResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
            };

            try
            {
                HttpResponseMessage postResult;

                using (var hc = clientFactory.CreateClient())
                {

                    AtlasCallSetStatusRequest pst = new AtlasCallSetStatusRequest
                    {
                        CallerEmail = parentGptwContext.Email,
                        SessionId = parentGptwContext.SessionId,
                        ClientId = clientId,
                        EngagementId = engagementId,
                        Cert = "",
                        List = ""
                    };

                    if (name.ToLower() == "cstatus")
                    {
                        pst.Cert = value;

                        if (pst.Cert.ToLower() == "not certified")
                        {
                            pst.List = "Not Eligible";
                        }
                    }
                    if (name.ToLower() == "lstatus")
                    {
                        pst.List = value;
                    }
                    if (name.ToLower() == "renewalstatus")
                    {
                        pst.Renewal = value;
                    }

                    AtlasLog.LogInformation(String.Format("Calling: AtlasSetStatus WebAPI. {0}", JsonConvert.SerializeObject(pst)), gptwContext);

                    pst.Username = appSettings.AtlasApiUserName;
                    pst.Password = appSettings.AtlasApiPassword;

                    String content = JsonConvert.SerializeObject(pst);

                    postResult = await hc.PostAsync(appSettings.ReportingURL + "/orchestration/setStatus", new StringContent(content, UnicodeEncoding.UTF8, "application/json"));
                }

                if (postResult.IsSuccessStatusCode)
                {
                    AtlasLog.LogInformation(String.Format("Returned Successfully from: AtlasSetStatus WebAPI."), gptwContext);

                    // EngagementStatus Call
                    if (name.ToLower() == "cstatus" && value.ToLower() == "not certified")
                    {
                        ECRV2 writeableECRV2 = this.AORepository.RetrieveReadWriteECRV2(Int32.Parse(engagementId));
                        if (writeableECRV2 != null)
                        {
                            AtlasLog.LogInformation(String.Format("Calling TakeNotCertifedActions"), gptwContext);
                            writeableECRV2.TakeNotCertifedActions();
                            this.AORepository.SaveECRV2(ref writeableECRV2);
                        }
                        else
                        {
                            AtlasLog.LogError(String.Format("ERROR:AtlasSetStatus failed to get a writeable ECRV2 for eid {0}.", engagementId), gptwContext);
                        }
                    }

                    string apiResponse = postResult.Content.ReadAsStringAsync().Result;

                    result = JsonConvert.DeserializeObject<AtlasCallSetStatusResult>(apiResponse);

                    return result;
                }
                else
                {
                    AtlasLog.LogError(String.Format("ERROR:AtlasSetStatus failed with an http status code of {0}.", postResult.StatusCode), gptwContext);
                    result.ErrorStr = "CallAtlasSetStatus failed with an http status code of " + postResult.StatusCode;
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorStr = "CallAtlasToSetContactLoginDate failed with an unhandled error of: " + e.Message;
            }

            return result;
        }


        [NonAction]
        private string MapClientIdToAffiliateId(int clientId)
        {
            string result = "";
            List<Affiliate> myAffiliates = this.AORepository.GetAllAffiliates().ToList();
            foreach (var affiliate in myAffiliates)
            {
                if (clientId >= affiliate.StartClientId && clientId <= affiliate.EndClientId)
                    return affiliate.AffiliateId;
            }
            return result;
        }

        [NonAction]
        private void GetCertificationDetails(GetEngagementInfoResult result, ECRV2 ecrv2)
        {
            List<CountryData> certcountries = this.AORepository.FindCertificationCountries(ecrv2);
            foreach (CountryData country in certcountries)
            {
                result.curCertInfo.certificationStartDate = ConvertToShortDateString(country.CertificationDate);
                result.curCertInfo.certificationExpiryDate = ConvertToShortDateString(country.CertificationExpiryDate);
                result.curCertInfo.empResultsDate = ReturnMonthYear(country.CertificationDate);
                result.curCertInfo.reportDownloadsDate = ReturnMonthYear(country.CertificationDate);
                result.curCertInfo.currentlyCertified = false;
                result.curCertInfo.continueToShowEmpAndReports = false;
                result.curCertInfo.trustIndexSSOLink = "";
                result.curCertInfo.profilePublishedLink = country.ProfilePublishedLink;
                result.curCertInfo.engagementId = ecrv2.EngagementId;

                if (country.CertificationExpiryDate != null)
                {
                    if (country.CertificationExpiryDate >= DateTime.Today.Date)
                        result.curCertInfo.currentlyCertified = true;
                    else
                    {
                        DateTime today = DateTime.Today;
                        DateTime pastDate = (DateTime)country.CertificationExpiryDate;
                        var numberOfDays = (today - pastDate).TotalDays;
                        if (numberOfDays < 6 * 30)
                            result.curCertInfo.continueToShowEmpAndReports = true;
                    }
                }
            }
        }

        [NonAction]
        private int FindEngagementId(int clientId, string eidToFind, GptwLogContext parentGptwContext)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("FindEngagementId", parentGptwContext);
            //(a)Find all ECRs with a create date in the last 365 days that are not abandoned.
            //(b)Add to this list all ECRs with a certified date in the last 365 days that are not abandoned.
            //(c)If there are 0 ECRs found, return { Found = false, Id = 0, State = '}
            //(d)Otherwise return the EngagementId with the most recent create date and state.

            try
            {
                List<ECRV2> AllECRV2sForClient = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);
                DateTime date365DaysAgo = DateTime.Now.AddDays(-365);
                List<ECRV2> ECRV2s = new List<ECRV2>();
                List<ECRV2> ECRV2Group1 = (from e in AllECRV2sForClient.AsQueryable() where (e.IsAbandoned == false && e.CreatedDate > date365DaysAgo) select e).ToList();
                if (ECRV2Group1.Count > 0)
                    ECRV2s.AddRange(ECRV2Group1);

                List<ECRV2> ECRV2Group2 = (from e in AllECRV2sForClient.AsQueryable()
                                           where (!e.IsAbandoned &&
                                           String.Equals(this.AORepository.FindCertificationCountries(e).Count > 0 ?
                                           this.AORepository.FindCertificationCountries(e)[0].CertificationStatus : "",
                                           "certified", StringComparison.OrdinalIgnoreCase) &&
                                           (this.AORepository.FindCertificationCountries(e).Count > 0 ?
                                           this.AORepository.FindCertificationCountries(e)[0].CertificationDate
                                           : DateTime.MinValue) > date365DaysAgo)
                                           select e).ToList();

                if (ECRV2Group2.Count > 0)
                    ECRV2s.AddRange(ECRV2Group2);


                if (ECRV2s.Count > 0)
                {
                    ECRV2s = (from e in ECRV2s.AsQueryable() orderby e.CreatedDate descending select e).ToList();

                    if (eidToFind == "CURRENT")
                        return ECRV2s[0].EngagementId;

                    if (ECRV2s.Count > 1 & eidToFind == "PREVIOUS")
                        return ECRV2s[1].EngagementId;
                }

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return 0;
        }

        /// <summary>
        /// Return the engagementId of the most recent non-abandoned certified engagement prior to the current engagement
        /// </summary>
        /// <param name="clientId">client id with the current engagement</param>
        /// <param name="currentEngagementId">current engagement id</param>
        /// <param name="parentGptwContext">logging context</param>
        /// <returns>engagementId of the most recent certified engagement before the current engagement
        /// Returns 0 if there is no previous certified engagement</returns>
        [NonAction]
        private int FindPreviousCertifiedEngagementId(int clientId, int currentEngagementId, GptwLogContext parentGptwContext)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("FindPreviousCertifiedEngagementId", parentGptwContext);

            int previousCertifiedEngagementId = 0;
            try
            {
                // Get all engagements for the client in descending order of creation date.
                List<ECRV2> AllECRV2sForClient = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);

                // Get all non-abandoned, certified engagements for the client that are not the current engagement.
                List<ECRV2> certifiedECRV2s = (from e in AllECRV2sForClient.AsQueryable()
                                               where (!e.IsAbandoned && e.EngagementId != currentEngagementId &&
                                                String.Equals(this.AORepository.FindCertificationCountries(e).Count > 0 ?
                                                    this.AORepository.FindCertificationCountries(e)[0].CertificationStatus : "",
                                                    "certified", StringComparison.OrdinalIgnoreCase))
                                               select e).ToList();

                if (certifiedECRV2s.Count > 0)
                {
                    // The first certified engagement is the most recent, prior to the current engagement
                    previousCertifiedEngagementId = certifiedECRV2s[0].EngagementId;
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return previousCertifiedEngagementId;
        }

        //[NonAction]
        //private async Task<SetPortalContactLoginDateResult> CallAtlasToSetContactLoginDate(int clientId, string email, GptwLogContext parentGptwContext)
        //{
        //    GptwLogContext gptwContext = GetNewGptwLogContext("CallAtlasToSetContactLoginDate", parentGptwContext);

        //    SetPortalContactLoginDateResult result = new SetPortalContactLoginDateResult
        //    {
        //        IsError = true,
        //        ErrorStr = "A general error has occurred.",
        //    };

        //    try
        //    {
        //        HttpResponseMessage postResult;

        //        using (var hc = clientFactory.CreateClient())
        //        {

        //            SetContactLoginDateRequest pst = new SetContactLoginDateRequest
        //            {
        //                Email = email,
        //                SessionId = parentGptwContext.SessionId,
        //                CallerEmail = parentGptwContext.Email,
        //                ClientId = clientId
        //            };

        //            AtlasLog.LogInformation(String.Format("Calling: Atlas setPortalContactLoginDate WebAPI. {0}", JsonConvert.SerializeObject(pst)), gptwContext);

        //            pst.Username = appSettings.AtlasApiUserName;
        //            pst.Password = appSettings.AtlasApiPassword;

        //            String content = JsonConvert.SerializeObject(pst);

        //            postResult = await hc.PostAsync(appSettings.ReportingURL + "/orchestration/setPortalContactLoginDate", new StringContent(content, UnicodeEncoding.UTF8, "application/json"));
        //        }

        //        if (postResult.IsSuccessStatusCode)
        //        {
        //            AtlasLog.LogInformation(String.Format("Returned Successfully from: Atlas setPortalContactLoginDate WebAPI."), gptwContext);

        //            string apiResponse = postResult.Content.ReadAsStringAsync().Result;

        //            result = JsonConvert.DeserializeObject<SetPortalContactLoginDateResult>(apiResponse);

        //            return result;
        //        }
        //        else
        //        {
        //            AtlasLog.LogError(String.Format("ERROR:Atlas Call ToSetContactLoginDate failed with an http status code of {0}.", postResult.StatusCode), gptwContext);
        //            result.ErrorStr = "CallAtlasToSetContactLoginDate failed with an http status code of " + postResult.StatusCode;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
        //        result.ErrorStr = "CallAtlasToSetContactLoginDate failed with an unhandled error of: " + e.Message;
        //    }

        //    return result;
        //}

        [NonAction]
        private async Task<CreateECRResult> CallAtlasToCreateECR(CreateECRRequest createECRRequest, GptwLogContext parentGptwContext)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("CallAtlasToCreateECR", parentGptwContext);
            AtlasLog.LogInformation(String.Format("CallAtlasToCreateECR: AffiliateId: '{0}', ClientId: '{1}', ClientName: '{2}', TrustIndexSurveyType: '{3}', CountryCode: '{4}', TotalEmployees: '{5}', CallerEmail: '{6}', ContactFirstName: '{7}', ContactLastName: '{8}', ContactEmail: '{9}'", createECRRequest.AffiliateId, createECRRequest.ClientId, createECRRequest.ClientName, createECRRequest.TrustIndexSurveyType, createECRRequest.CountryCode, createECRRequest.TotalEmployees, createECRRequest.CallerEmail, createECRRequest.FirstName, createECRRequest.LastName, createECRRequest.Email));

            CreateECRResult result = new CreateECRResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                ErrorId = ERROR_ID_NOT_DEFINED,
                WarningStr = "",
                EngagementId = -1
            };

            try
            {
                HttpResponseMessage postResult;

                using (var hc = clientFactory.CreateClient())
                {
                    createECRRequest.Username = appSettings.AtlasApiUserName;
                    createECRRequest.Password = appSettings.AtlasApiPassword;

                    String content = JsonConvert.SerializeObject(createECRRequest);

                    postResult = await hc.PostAsync(appSettings.ReportingURL + "/orchestration/createecr", new StringContent(content, UnicodeEncoding.UTF8, "application/json"));
                }

                if (postResult.IsSuccessStatusCode)
                {
                    string apiResponse = postResult.Content.ReadAsStringAsync().Result;

                    result = JsonConvert.DeserializeObject<CreateECRResult>(apiResponse);

                    return result;
                }
                else
                {
                    AtlasLog.LogError(String.Format("ERROR:Atlas call failed with an http status code of {0}.", postResult.StatusCode), gptwContext);
                    result.ErrorStr = "CreateECR failed with an http status code of " + postResult.StatusCode;
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorStr = "CreateECR failed with an unhandled error of: " + e.Message;
            }

            return result;
        }

        [NonAction]
        private List<ClientRecordDetail> RetrieveFromBDS(int clientId, GptwLogContext parentGptwContext)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("RetrieveFromBDS", parentGptwContext);

            List<ClientRecordDetail> retrieveFromBDSResult = new List<ClientRecordDetail>();

            try
            {
                HttpResponseMessage result;

                using (var hc = clientFactory.CreateClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                    string url = appSettings.BusinessDataServicesURL + "/api/client/getDClientRecords";
                    //   string url = appSettings.BusinessDataServicesURL + "/api/client/getListCalendar";

                    GetDClientRecordsRequest pst = new GetDClientRecordsRequest
                    {
                        token = appSettings.BusinessDataServicesToken,
                        cid = clientId
                    };

                    String content = JsonConvert.SerializeObject(pst);

                    if (content == null)
                    {
                        AtlasLog.LogError(String.Format("ERROR:RetrieveFromBDS content = null."), gptwContext);
                        return new List<ClientRecordDetail>();
                    }

                    StringContent StuffToPost = new StringContent(content);
                    StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    result = hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json")).Result;
                }

                if (result.IsSuccessStatusCode)
                {
                    string apiResponse = result.Content.ReadAsStringAsync().Result;
                    var records = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(apiResponse);

                    for (int x = 0; x < records.Count; x++)
                    {
                        ClientRecordDetail clientRecordDetail = new ClientRecordDetail();
                        string str;
                        if (records[x].TryGetValue("survey_ver_id", out str))
                            clientRecordDetail.survey_ver_id = Int32.Parse(str);
                        if (records[x].TryGetValue("client_short_name", out str))
                            clientRecordDetail.client_short_name = str;
                        if (records[x].TryGetValue("client_long_name", out str))
                            clientRecordDetail.client_long_name = str;
                        if (records[x].TryGetValue("create_dt", out str) && str != "$NULL$")
                            clientRecordDetail.create_dt = DateTime.Parse(str);
                        retrieveFromBDSResult.Add(clientRecordDetail);
                    }

                }
                else
                {
                    AtlasLog.LogError(String.Format("ERROR:RetrieveFromBDS failed with an http status code of {0}.", result.StatusCode), gptwContext);
                    retrieveFromBDSResult = new List<ClientRecordDetail>();
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                retrieveFromBDSResult = new List<ClientRecordDetail>();
            }

            return retrieveFromBDSResult;
        }

        [NonAction]
        private string ConvertToShortDateString(DateTime? dateToConvert)
        {
            string result = "";
            try
            {
                string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

                if (dateToConvert == null)
                    result = "";
                else
                {
                    DateTime d = (DateTime)dateToConvert;
                    result = months[d.Month - 1] + " " + d.Day + ", " + d.Year;
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("ConvertToShortDateString Exception caught."), e);
            }
            return result;
        }

        [NonAction]
        private string ReturnMonthYear(DateTime? dateToConvert)
        {
            string result = "";
            try
            {
                string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

                if (dateToConvert == null)
                    result = "";
                else
                {
                    DateTime d = (DateTime)dateToConvert;
                    result = months[d.Month - 1] + " " + d.Year;
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("ReturnMonthYear Exception caught."), e);
            }
            return result;
        }

        [NonAction]
        private string NormalizeCertificationStatus(string certificationStatus)
        {
            string result = "";
            try
            {
                if (string.IsNullOrEmpty(certificationStatus))
                    result = "";
                else
                    result = certificationStatus;
                result = result.ToLower();
                result = result.Replace(" ", ""); // remove spaces not certified to notcertified
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("NormalizeCertificationStatus Exception caught."), e);
            }
            return result;
        }

        [NonAction]
        // returns a boolean indicating that this call succeeded
        private bool FindEligibleCSToImageEdit(int clientId, out int engagementId, GptwLogContext parentGptwContext)
        {
            engagementId = -1;

            GptwLogContext gptwContext = GetNewGptwLogContext("FindElibleCSToImageEdit", parentGptwContext);

            try
            {
                List<ECRV2> AllECRV2sForClient = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);
                DateTime date2YearsAgo = DateTime.Now.AddDays(-365 * 2);
                List<ECRV2> ECRV2s = new List<ECRV2>();
                // TODO is Group1 necessary here?
                List<ECRV2> ECRV2Group1 = (from e in AllECRV2sForClient.AsQueryable() where (e.IsAbandoned == false && e.CreatedDate > date2YearsAgo) select e).ToList();
                if (ECRV2Group1.Count > 0)
                    ECRV2s.AddRange(ECRV2Group1);
                List<ECRV2> ECRV2Group2 = (from e in AllECRV2sForClient.AsQueryable()
                                           where (!e.IsAbandoned && String.Equals(this.AORepository.FindUSCountryData(e).CertificationStatus, "certified", StringComparison.OrdinalIgnoreCase) && this.AORepository.FindUSCountryData(e).CertificationDate > date2YearsAgo)
                                           select e).ToList();
                if (ECRV2Group2.Count > 0)
                    // Both groups are extracts from AllECRV2sForClient, so default Comparer works here, comparison is by reference
                    ECRV2s.AddRange(ECRV2Group2.Except(ECRV2Group1));

                if (ECRV2s.Count > 0)
                {
                    ECRV2s = (from e in ECRV2s.AsQueryable() orderby e.CreatedDate descending select e).ToList();

                    for (int index = 0; index < ECRV2s.Count; index++)
                    {
                        CountryData country = this.AORepository.FindUSCountryData(ECRV2s[index]);

                        // WARNING if the CB were manually re-opened and not closed, this logic will skip over this engagement. That may not be the desired result.
                        if (String.Compare(ECRV2s[index].ECR.CultureBriefStatus, "COMPLETED", true) == 0)
                        {
                            if (String.Compare(country.ProfilePublishStatus, "SUCCESS", true) == 0)
                            {
                                engagementId = ECRV2s[index].EngagementId;
                                return true;
                            }
                            if (String.Compare(country.ProfilePublishStatus, "REQUESTED", true) == 0)
                            {
                                engagementId = -2;
                                return true;
                            }
                            if (String.Compare(country.ProfilePublishStatus, "FAILURE", true) == 0)
                            {
                                engagementId = -3;
                                return true;
                            }
                        }

                    }
                }

                return true;

            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return false;
        }

        [NonAction]
        public async Task<RepublishProfileResult> CallAtlasToRepublishProfile(RepublishProfileRequest republishProfileRequest, GptwLogContext parentGptwContext)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("CallAtlasToRepublishProfile", parentGptwContext);

            RepublishProfileResult result = new RepublishProfileResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
            };

            try
            {
                HttpResponseMessage postResult;

                using (var hc = clientFactory.CreateClient())
                {
                    republishProfileRequest.Username = appSettings.AtlasApiUserName;
                    republishProfileRequest.Password = appSettings.AtlasApiPassword;

                    String content = JsonConvert.SerializeObject(republishProfileRequest);

                    postResult = await hc.PostAsync(appSettings.ReportingURL + "/orchestration/republishprofile", new StringContent(content, UnicodeEncoding.UTF8, "application/json"));
                }

                if (postResult.IsSuccessStatusCode)
                {
                    string apiResponse = postResult.Content.ReadAsStringAsync().Result;

                    result = JsonConvert.DeserializeObject<RepublishProfileResult>(apiResponse);

                    return result;
                }
                else
                {
                    AtlasLog.LogError(String.Format("ERROR:Atlas call to republish profile failed with an http status code of {0}.", postResult.StatusCode), gptwContext);
                    result.ErrorStr = "CallAtlasToRepublishProfile failed with an http status code of " + postResult.StatusCode;
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorStr = "Call to Atlas to republish profile failed with an unhandled error of: " + e.Message;
            }

            return result;
        }

        [NonAction]
        private GeneralCallResult IsClientIdReadyForNewECR(int clientId, GptwLogContext gptwContext)
        {
            var result = new GeneralCallResult() { ErrorMessage = "An unhandled error occurred.", Success = false };

            try
            {
                List<ECRV2> ecrv2s = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);

                AtlasLog.LogInformation(String.Format("Found no ECR's for clientId:{0} so allowing ECRCreation.", clientId), gptwContext);
                if (ecrv2s.Count == 0)
                {
                    result.ErrorMessage = "";
                    result.Success = true;
                    return result;
                }

                List<ECRV2> orderByDescendingECRs = (from ecrv2 in ecrv2s
                                                     orderby ecrv2.CreatedDate descending
                                                     select ecrv2).ToList();

                AtlasLog.LogInformation(string.Format("Found {0} ECR's for clientId:{1}.", orderByDescendingECRs.Count, clientId), gptwContext);

                ECRV2 mostRecentECRV2 = orderByDescendingECRs[0];

                AtlasLog.LogInformation(string.Format("An Ecr:{0} with CreateDate:{1} for clientId:{2} was found to be the most recent.", mostRecentECRV2.EngagementId, mostRecentECRV2.CreatedDate, clientId), gptwContext);

                if ((mostRecentECRV2.IsAbandoned))
                {
                    AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} was found Abandoned so we are allowing Creation.", mostRecentECRV2.EngagementId, clientId), gptwContext);

                    result.ErrorMessage = "";
                    result.Success = true;
                    return result;
                }

                var RODOS_RELEASE_DATE = System.DateTime.Parse("9/18/2018"); // RODOS was released on the night of 9/17/2019 as near as I can tell. So POST RODOS CREATE DATES of 9/18/2019 should be good

                if ((mostRecentECRV2.CreatedDate < RODOS_RELEASE_DATE))
                {
                    AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} was found to have a PreRODOS CreateDate so we are allowing ECR Creation.", mostRecentECRV2.EngagementId, clientId), gptwContext);
                    result.ErrorMessage = "";
                    result.Success = true;
                    return result;
                }

                bool TIOptOutOrAbandonedOccurred = false;
                bool CBOptOutOrAbandonedOccurred = false;


                // Is TI complete?
                if ((!string.IsNullOrEmpty(mostRecentECRV2.ECR.TrustIndexSSOLink)))
                {
                    string TIStatus = mostRecentECRV2.ECR.TrustIndexStatus;

                    AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} has TrustIndexStatus:{2}.", mostRecentECRV2.EngagementId, clientId, TIStatus), gptwContext);
                    if (string.Compare(TIStatus, "abandoned", StringComparison.OrdinalIgnoreCase) == 0 | string.Compare(TIStatus, "opted-out", StringComparison.OrdinalIgnoreCase) == 0)
                        TIOptOutOrAbandonedOccurred = true;

                    if (!(string.Compare(TIStatus, "data loaded", StringComparison.OrdinalIgnoreCase) == 0) & !TIOptOutOrAbandonedOccurred)
                    {
                        result.ErrorMessage = string.Format("The most recent ECR:{0} created for clientId:{1}, which was created on {2} has a TI Status of '{3}' which means that it's incomplete. Therefore new ECRs can't be created for this client until the TI survey has been completed (TrustIndexStatus set to data loaded, opted-out or abandoned).", mostRecentECRV2.EngagementId, clientId, mostRecentECRV2.CreatedDate, TIStatus);
                        result.Success = false;
                        return result;
                    }
                }

                // Is CA complete?
                if ((!string.IsNullOrEmpty(mostRecentECRV2.ECR.CultureAuditSSOLink)))
                {
                    string CAStatus = mostRecentECRV2.ECR.CultureAuditStatus;

                    AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} has CAStatus:{2}.", mostRecentECRV2.EngagementId, clientId, CAStatus), gptwContext);

                    if (!(string.Compare(CAStatus, "completed", StringComparison.OrdinalIgnoreCase) == 0) & !(string.Compare(CAStatus, "abandoned", StringComparison.OrdinalIgnoreCase) == 0) & !(string.Compare(CAStatus, "opted-out", StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        result.ErrorMessage = string.Format("The most recent ECR:{0} created for clientId:{1}, which was created on {2} has a CultureAudit link:{3} and a CultureAuditStatus of '{4}' which means that it isn't yet complete. Therefore new ECRs can't be created for this client until the CultureAudit is finished and has been set to completed , opted-out or abandoned.", mostRecentECRV2.EngagementId, clientId, mostRecentECRV2.CreatedDate, mostRecentECRV2.ECR.CultureAuditSSOLink, CAStatus);
                        result.Success = false;
                        return result;
                    }
                }

                // Is CB complete?
                if ((!string.IsNullOrEmpty(mostRecentECRV2.ECR.CultureBriefSSOLink)))
                {
                    string CBStatus = mostRecentECRV2.ECR.CultureBriefStatus;

                    AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} has CBStatus:{2}.", mostRecentECRV2.EngagementId, clientId, CBStatus), gptwContext);

                    if (string.Compare(CBStatus, "abandoned", StringComparison.OrdinalIgnoreCase) == 0 | string.Compare(CBStatus, "opted-out", StringComparison.OrdinalIgnoreCase) == 0)
                        CBOptOutOrAbandonedOccurred = true;

                    if (!(string.Compare(CBStatus, "completed", StringComparison.OrdinalIgnoreCase) == 0) & !CBOptOutOrAbandonedOccurred)
                    {
                        result.ErrorMessage = string.Format("The most recent ECR:{0} created for clientId:{1}, which was created on {2} has a CultureBrief link:{3} and a CultureBriefStatus of '{4}' which means that it isn't yet complete. Therefore new ECRs can't be created for this client until the CultureBrief is finished and has been set to completed, opted-out or abandoned.", mostRecentECRV2.EngagementId, clientId, mostRecentECRV2.CreatedDate, mostRecentECRV2.ECR.CultureBriefSSOLink, CBStatus);
                        result.Success = false;
                        return result;
                    }
                }

                // if the TI is none or the TI/CB is set to Opted-Out or Abandoned I will NOT check the CertificationStatus or ListEligibilityStatus. The reasoning is that they can never get certified or be eligible for lists
                if (string.IsNullOrEmpty(mostRecentECRV2.ECR.TrustIndexSSOLink) | TIOptOutOrAbandonedOccurred | CBOptOutOrAbandonedOccurred)
                {
                    result.ErrorMessage = "";
                    result.Success = true;
                    return result;
                }

                CountryData certCountry = this.AORepository.FindCertificationCountry(mostRecentECRV2);
                if (certCountry.IsApplyingForCertification == "Yes")
                {

                    // Has the certification decision been made?
                    string CertificationStatus = certCountry.CertificationStatus;

                    AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} has CertificationStatus:{2}.", mostRecentECRV2.EngagementId, clientId, CertificationStatus), gptwContext);

                    if (!(string.Compare(CertificationStatus, "certified", StringComparison.OrdinalIgnoreCase) == 0) & !(string.Compare(CertificationStatus, "not certified", StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        result.ErrorMessage = string.Format("The most recent ECR:{0} created for clientId:{1}, which was created on {2} has a CertificationStatus of '{3}' which means that the Certification decision hasn't been made. Therefore new ECRs can't be created for this client until the CertificationStatus has been changed to 'certified' or 'not certified'.", mostRecentECRV2.EngagementId, clientId, mostRecentECRV2.CreatedDate, CertificationStatus);
                        result.Success = false;
                        return result;
                    }

                    // Has the list eligibility decision been made?
                    string ListEligibilityStatus = certCountry.ListEligibilityStatus;

                    AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} has ListEligibilityStatus:{2}.", mostRecentECRV2.EngagementId, clientId, ListEligibilityStatus), gptwContext);

                    if (!(string.Compare(ListEligibilityStatus, "eligible", StringComparison.OrdinalIgnoreCase) == 0) & !(string.Compare(ListEligibilityStatus, "not eligible", StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        result.ErrorMessage = string.Format("The most recent ECR:{0} created for clientId:{1}, which was created on {2} has a ListEligibilityStatus of '{3}' which means that the List Eligibility decision hasn't been made. Therefore new ECRs can't be created for this client until ListEligibilityStatus has been changed to 'eligible' or 'not eligible'.", mostRecentECRV2.EngagementId, clientId, mostRecentECRV2.CreatedDate, ListEligibilityStatus);
                        result.Success = false;
                        return result;
                    }
                }

                // if the mostRecentECRV2 has renewal status of Too Early, we do Not allow to create a New ECR
                if (mostRecentECRV2.RenewalStatus == RenewalStatus.TOOEARLY)
                {
                    result.ErrorMessage = string.Format("A new engagement can not be created if the most recent non abandoned ECR (EID: {0}) has a renewal status ({1}) of Too Early.", mostRecentECRV2.EngagementId, mostRecentECRV2.RenewalStatus);
                    result.Success = false;
                    return result;
                }

                result.ErrorMessage = "";
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"An unhandled exception has occured. Error: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        [NonAction]
        private void AddClient(GptwLogContext gptwContext, CreateECRRequest createECRRequest)
        {
            if (!IsClientExist(gptwContext, createECRRequest.ClientId))
            {
                try
                {
                    AtlasLog.LogInformation("Portal Calling BDS:AddClient WebAPI", gptwContext);
                    HttpResponseMessage postResult;

                    using (var hc = clientFactory.CreateClient())
                    {
                        hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                        string url = appSettings.BusinessDataServicesURL + "/api/client/addClient";

                        var pst = new
                        {
                            token = appSettings.BusinessDataServicesToken,
                            cid = createECRRequest.ClientId,
                            svid = 0,
                            client_short_name = createECRRequest.ClientName,
                            client_long_name = createECRRequest.ClientName,
                            affiliateId = createECRRequest.AffiliateId
                        };

                        String content = JsonConvert.SerializeObject(pst);

                        postResult = hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json")).Result;
                    }

                    AtlasLog.LogInformation("Returned from call to BDS:AddClient WebAPI", gptwContext);
                }
                catch (Exception e)
                {
                    AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                }
            }
        }

        [NonAction]
        private bool IsClientExist(GptwLogContext gptwContext, int clientId)
        {
            bool isExist = false;

            try
            {
                AtlasLog.LogInformation("Portal Calling BDS:GetAffiliateIdByClientId WebAPI", gptwContext);
                HttpResponseMessage postResult;

                using (var hc = clientFactory.CreateClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                    string url = appSettings.BusinessDataServicesURL + "/api/client/getaffiliateidbyclientid";

                    var pst = new
                    {
                        token = appSettings.BusinessDataServicesToken,
                        cid = clientId
                    };

                    String content = JsonConvert.SerializeObject(pst);

                    postResult = hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json")).Result;

                    if (postResult.IsSuccessStatusCode)
                    {
                        string apiResponse = postResult.Content.ReadAsStringAsync().Result;
                        var result = JsonConvert.DeserializeObject<JArray>(apiResponse);

                        if (result != null && result.Count > 0)
                            isExist = true;
                    }
                }

                AtlasLog.LogInformation("Returned from call to BDS:GetAffiliateIdByClientId WebAPI", gptwContext);
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
            }

            return isExist;
        }

        [NonAction]
        private async Task<CreateECRResult> CallAtlasToCreateEcr2(CreateEcrRequest2 createEcrRequest, GptwLogContext parentGptwContext)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("CallAtlasToCreateECR2", parentGptwContext);
            AtlasLog.LogInformation(String.Format("CallAtlasToCreateECR2: AffiliateId: '{0}', ClientId: '{1}', ClientName: '{2}', TrustIndexSurveyType: '{3}', CountryCode: '{4}', TotalEmployees: '{5}', CallerEmail: '{6}', ContactFirstName: '{7}', ContactLastName: '{8}', ContactEmail: '{9}'",
                createEcrRequest.AffiliateId, createEcrRequest.ClientId, createEcrRequest.ClientName, createEcrRequest.TrustIndexSurveyType,
                createEcrRequest.CountryCode, createEcrRequest.TotalEmployees, createEcrRequest.CallerEmail, createEcrRequest.FirstName,
                createEcrRequest.LastName, createEcrRequest.Email));

            CreateECRResult result = new CreateECRResult
            {
                IsError = true,
                ErrorStr = "A general error has occurred.",
                ErrorId = ERROR_ID_NOT_DEFINED,
                WarningStr = "",
                EngagementId = -1
            };

            try
            {
                HttpResponseMessage postResult;

                using (var hc = clientFactory.CreateClient())
                {
                    createEcrRequest.Username = appSettings.AtlasApiUserName;
                    createEcrRequest.Password = appSettings.AtlasApiPassword;

                    String content = JsonConvert.SerializeObject(createEcrRequest);

                    var url = appSettings.ReportingURL + "/orchestration/createecr2";
                    postResult = await hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json"));
                }

                if (postResult.IsSuccessStatusCode)
                {
                    string apiResponse = postResult.Content.ReadAsStringAsync().Result;

                    result = JsonConvert.DeserializeObject<CreateECRResult>(apiResponse);

                    return result;
                }
                else
                {
                    AtlasLog.LogError(String.Format("ERROR:Atlas call failed with an http status code of {0}.", postResult.StatusCode), gptwContext);
                    result.ErrorStr = "CreateECR2 failed with an http status code of " + postResult.StatusCode;
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                result.ErrorStr = "CreateECR2 failed with an unhandled error of: " + e.Message;
            }

            return result;
        }

        [NonAction]
        private void AddClient2(GptwLogContext gptwContext, CreateEcrRequest2 createECRRequest)
        {
            if (!IsClientExist(gptwContext, createECRRequest.ClientId))
            {
                try
                {
                    AtlasLog.LogInformation("Portal Calling BDS:AddClient2 WebAPI", gptwContext);
                    HttpResponseMessage postResult;

                    using (var hc = clientFactory.CreateClient())
                    {
                        hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                        string url = appSettings.BusinessDataServicesURL + "/api/client/addClient";

                        var pst = new
                        {
                            token = appSettings.BusinessDataServicesToken,
                            cid = createECRRequest.ClientId,
                            svid = 0,
                            client_short_name = createECRRequest.ClientName,
                            client_long_name = createECRRequest.ClientName,
                            affiliateId = createECRRequest.AffiliateId
                        };

                        String content = JsonConvert.SerializeObject(pst);

                        postResult = hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json")).Result;
                    }

                    AtlasLog.LogInformation("Returned from call to BDS:AddClient2 WebAPI", gptwContext);
                }
                catch (Exception e)
                {
                    AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                }
            }
        }

        [NonAction]
        private GeneralCallResult IsClientIdReadyForNewECR2(int clientId, bool isCreateCA, List<string> newEcrCountryCodeList, GptwLogContext gptwContext)
        {
            var result = new GeneralCallResult() { ErrorMessage = "An unhandled error occurred.", Success = false };

            try
            {
                List<ECRV2> ecrv2s = this.AORepository.RetrieveReadOnlyECRV2sByClientId(clientId);

                AtlasLog.LogInformation(String.Format("Found no ECR's for clientId:{0} so allowing ECRCreation.", clientId), gptwContext);
                if (ecrv2s.Count == 0)
                {
                    result.ErrorMessage = "";
                    result.Success = true;
                    return result;
                }

                AtlasLog.LogInformation(string.Format("Found {0} ECR's for clientId:{1}.", ecrv2s.Count, clientId), gptwContext);

                foreach (ECRV2 ecrv2 in ecrv2s.Where(p => !p.IsAbandoned && p.CreatedDate <= p.CreatedDate.AddYears(1)))
                {
                    AtlasLog.LogInformation(string.Format("An Ecr:{0} with CreateDate:{1} for clientId:{2} was found.", ecrv2.EngagementId, ecrv2.CreatedDate, clientId), gptwContext);

                    bool TIOptOutOrAbandonedOccurred = false;
                    bool CBOptOutOrAbandonedOccurred = false;
                    var ecrv2CountryCodeList = new List<string>();
                    if (ecrv2.ECR.Countries != null && ecrv2.ECR.Countries.Count > 0)
                        ecrv2CountryCodeList = ecrv2.ECR.Countries.Select(p => p.CountryCode.Trim().ToUpper()).ToList();

                    var isCountryExisting = newEcrCountryCodeList.Any(ecrv2CountryCodeList.Contains);

                    // Is TI complete?
                    if ((!string.IsNullOrEmpty(ecrv2.ECR.TrustIndexSSOLink)))
                    {
                        string TIStatus = ecrv2.ECR.TrustIndexStatus;

                        AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} has TrustIndexStatus:{2}.", ecrv2.EngagementId, clientId, TIStatus), gptwContext);
                        if (string.Compare(TIStatus, "abandoned", StringComparison.OrdinalIgnoreCase) == 0 | string.Compare(TIStatus, "opted-out", StringComparison.OrdinalIgnoreCase) == 0)
                            TIOptOutOrAbandonedOccurred = true;

                        if (!(string.Compare(TIStatus, "data loaded", StringComparison.OrdinalIgnoreCase) == 0) & !TIOptOutOrAbandonedOccurred &
                            isCountryExisting)
                        {
                            result.ErrorMessage = string.Format("The most recent ECR:{0} created for clientId:{1} with countries: {2}, which was created on {3} has a TI Status of '{4}' which means that it's incomplete. Therefore new ECRs can't be created for this client until the TI survey has been completed (TrustIndexStatus set to data loaded, opted-out or abandoned) for these countries.",
                                ecrv2.EngagementId, clientId, string.Join(", ", ecrv2CountryCodeList), ecrv2.CreatedDate, TIStatus);
                            result.Success = false;
                            return result;
                        }
                    }

                    // Is CA complete?
                    if (isCreateCA && (!string.IsNullOrEmpty(ecrv2.ECR.CultureAuditSSOLink)))
                    {
                        string CAStatus = ecrv2.ECR.CultureAuditStatus;

                        AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} has CAStatus:{2}.", ecrv2.EngagementId, clientId, CAStatus), gptwContext);

                        if (!(string.Compare(CAStatus, "completed", StringComparison.OrdinalIgnoreCase) == 0) & !(string.Compare(CAStatus, "abandoned", StringComparison.OrdinalIgnoreCase) == 0) & !(string.Compare(CAStatus, "opted-out", StringComparison.OrdinalIgnoreCase) == 0))
                        {
                            result.ErrorMessage = string.Format("The most recent ECR:{0} created for clientId:{1}, which was created on {2} has a CultureAudit link:{3} and a CultureAuditStatus of '{4}' which means that it isn't yet complete. Therefore new ECRs can't be created for this client until the CultureAudit is finished and has been set to completed , opted-out or abandoned.",
                                ecrv2.EngagementId, clientId, ecrv2.CreatedDate, ecrv2.ECR.CultureAuditSSOLink, CAStatus);
                            result.Success = false;
                            return result;
                        }
                    }

                    // Is CB complete?
                    if ((!string.IsNullOrEmpty(ecrv2.ECR.CultureBriefSSOLink)))
                    {
                        string CBStatus = ecrv2.ECR.CultureBriefStatus;

                        AtlasLog.LogInformation(string.Format("Most Recent Ecr:{0} for clientId:{1} has CBStatus:{2}.", ecrv2.EngagementId, clientId, CBStatus), gptwContext);

                        if (string.Compare(CBStatus, "abandoned", StringComparison.OrdinalIgnoreCase) == 0 | string.Compare(CBStatus, "opted-out", StringComparison.OrdinalIgnoreCase) == 0)
                            CBOptOutOrAbandonedOccurred = true;

                        if (!(string.Compare(CBStatus, "completed", StringComparison.OrdinalIgnoreCase) == 0) & !CBOptOutOrAbandonedOccurred &
                            isCountryExisting)
                        {
                            result.ErrorMessage = string.Format("The most recent ECR:{0} created for clientId:{1} with countries: {2}, which was created on {3} has a CultureBrief link:{4} and a CultureBriefStatus of '{5}' which means that it isn't yet complete. Therefore new ECRs can't be created for this client until the CultureBrief is finished and has been set to completed, opted-out or abandoned.",
                                ecrv2.EngagementId, clientId, string.Join(", ", ecrv2CountryCodeList), ecrv2.CreatedDate, ecrv2.ECR.CultureBriefSSOLink, CBStatus);
                            result.Success = false;
                            return result;
                        }
                    }

                    // if the TI is none or the TI/CB is set to Opted-Out or Abandoned I will NOT check the CertificationStatus or ListEligibilityStatus. The reasoning is that they can never get certified or be eligible for lists
                    if (string.IsNullOrEmpty(ecrv2.ECR.TrustIndexSSOLink) | TIOptOutOrAbandonedOccurred | CBOptOutOrAbandonedOccurred)
                    {
                        result.ErrorMessage = "";
                        result.Success = true;
                        return result;
                    }
                }

                result.ErrorMessage = "";
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"An unhandled exception has occured. Error: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        [NonAction]
        private (TableOfContentStatus Status, TableOfContentSection Section) GetTbcCultureBriefStatusAndSection(string cbStatus, string tiStatus)
        {
            TableOfContentStatus tbcStatus = TableOfContentStatus.Hidden;
            TableOfContentSection tbcSection = TableOfContentSection.Hidden;

            cbStatus = cbStatus.ToLower();
            tiStatus = tiStatus.ToLower();


            if ((cbStatus.Equals("in progress", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("in progress", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("created", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("created", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase)))
            {
                tbcStatus = TableOfContentStatus.Hidden;
                tbcSection = TableOfContentSection.Hidden;
            }
            else if ((cbStatus.Equals("created", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("created", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("ready to launch", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("created", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("created", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("created", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("setup in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("created", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("data loaded", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("created", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey closed", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("ready to launch", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("created", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("setup in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("data loaded", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("in progress", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("in progress", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("ready to launch", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("in progress", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("created", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("in progress", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("setup in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("in progress", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("data loaded", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("in progress", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey closed", StringComparison.OrdinalIgnoreCase)))
            {
                tbcStatus = TableOfContentStatus.In_Progress;
                tbcSection = TableOfContentSection.OpenApplication;
            }
            else if ((cbStatus.Equals("created", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("created", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("ready to launch", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("setup in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("data loaded", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("abandoned", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey closed", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("ready to launch", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("setup in progress", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("data loaded", StringComparison.OrdinalIgnoreCase)) ||
                (cbStatus.Equals("opted-out", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey closed", StringComparison.OrdinalIgnoreCase)))
            {
                tbcStatus = TableOfContentStatus.Incomplete;
                tbcSection = TableOfContentSection.SubmittedApplication;
            }
            else if (cbStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) && tiStatus.Equals("survey closed", StringComparison.OrdinalIgnoreCase))
            {
                tbcStatus = TableOfContentStatus.Complete;
                tbcSection = TableOfContentSection.SubmittedApplication;
            }

            return (tbcStatus, tbcSection);
        }

        [NonAction]
        private TableOfContentStatus GetTbcCultureAuditStatus(string caStatus)
        {
            switch (caStatus.ToLower())
            {
                case "created":
                    return TableOfContentStatus.Not_Started;
                case "in progress":
                    return TableOfContentStatus.In_Progress;
                case "completed":
                    return TableOfContentStatus.Complete;
                default:
                    return TableOfContentStatus.Hidden;

            }
        }
    }
}