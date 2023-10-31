using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedProject2;
using Microsoft.Azure.Amqp.Framing;

namespace Portal.Model
{
    public class AppSettings
    {
        public string Environment { get; set; }
        public string AuthServerUserInfoEndpoint { get; set; }
        public string AuthServerApiName { get; set; }
        public string MongoLockAzureStorageConnectionString { get; set; }
        public string MongoDBConnectionString { get; set; }
        public int HttpClientTimeoutSeconds { get; set; }
        public string BusinessDataServicesURL { get; set; }
        public string BusinessDataServicesToken { get; set; }
        public string ReportingURL {
            get; 
            set; 
        }
        public string AtlasApiUserName { get; set; }
        public string AtlasApiPassword { get; set; }
        public string ReportStoreUrl { get; set; }
        public string ReportStoreServicesToken { get; set; }
        public string ReportManagerUrl { get; set; }
        public string ClientAssetsBlobStorageConnectionString { get; set; }
        public string DataConnectionString { get; set; }
        public string ServiceBusLogConnectionString { get; set; }
        public string ZenDeskSharedSecret { get; set; }
        public string ZenDeskUrl { get; set; }
        public string ZendeskAPIUrl { get; set; }
        public string ZendeskUsername { get; set; }
        public string ZendeskPassword { get; set; }
        public long? ZendeskRequesterId { get; set; }
        public long? ZendeskGroupId { get; set; }
        public int GPTWClientId { get; set; }
        public string LargeSmtpHost { get; set; }
        public string SmtpHost { get; set; }
        public string SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpFromAddress { get; set; }
        public string AzureStorageHeaderImage { get; set; }
        public string AppBuildNumber { get; set; }
        public string FileValidationWebApiUrl { get; set; }
        public string FileValidationWebApiSecret { get; set; }
        public string FileValidationBlobSASURI { get; set; }
        public string FileValidationClientId { get; set; }
        // Client Only- These are the only properties exposed to the client
        // We nned to be extra careful here because the client is insecure and we only want to expose things
        // which aren't secret and therefore should be kept on the server.

        public string AuthServerAuthority { get; set; }
        public string ApplicationInsightsKey { get; set; }
        public string GPTWWebSiteBaseUrl { get; set; }
        public string GPTWStoreUrl { get; set; }
        public string EmprisingURL { get; set; }
        public string IsAppMaintenanceMode { get; set; }
        public string ExpectedEnvironmentClaim { get; set; }
    }

    public class GetClientEmailDataResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public List<ClientEmail> data = new List<ClientEmail>();
    }

    public class ClientEmail
    {
        public string Id;
        public string EmailType;
        public int ClientId;
        public int EngagementId;
        public DateTime DateTimeSent;
        public string Subject;
        public string Body;
        public string Address;
        public bool Opened;
        public DateTime dateTimeOpened;
        public List<DateTime> dateTimeOpenedList = new List<DateTime>();
        public bool IsError;
        public string ErrorMessage;
    }
    public class GetDataRequestDataResult
    {
        public bool IsError;
        public string ErrorStr = "";
        //public List<DataRequest> data = new List<DataRequest>();
    }


    public class GetDataColRequest
    {
        public string token;
        public int cid;
        public int svid;
        public string filter;
    }

    public class GetDClientRecordsRequest
    {
        public string token;
        public int cid;
        public int svid;
    }

    public class GetNextListCerfificationRequest
    {
        public string token;
    }

    public class ClientRecordDetail
    {
        public int survey_ver_id;
        public string client_short_name;
        public string client_long_name;
        public DateTime create_dt;
    }

    public class Claim
    {
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
    }

    public class FindCurrentEngagementInfoResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public int ErrorId = 0;
        public bool FoundActiveEngagement;
        public int EngagementId = -1;
        public string CertificationStatus = "";
        public string Misc = "";
        public int ClientId = -1;
        public string ClientName = "";
    }

    public class GetCertAPPInfoResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public int ErrorId = 0;
        public List<CertificationApplication> OpenApplications = new List<CertificationApplication>();
        public List<CertificationApplication> SubmittedApplications = new List<CertificationApplication>();
    }

    public class CertificationApplication
    {
        public int EngagementId = -1;
        public string CBStatus = "";
        public string CAStatus = "";
        public DateTime CreateDate;
        public string CreateDateDisplay;
        public string Countries = "";
        public int CountryCount = 0;
        public bool IsCA = false;
        public bool IsCB = false;
        public string CultureAuditLink = "";
    }

    public class CertAppCountryInfo
    {
        public string Name = "";
        public bool IsApplyingForCertification;
    }

    enum TICBStatus
    {
        InProgress,
        Complete,
        InComplete
    }

    enum CAStatus
    {
        Created,
        InProgress,
        Complete
    }

    public class GetCompanyNameResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public string CompanyName = "";
    }

    public class FavoriteClickedResult
    {
        public bool IsError;
        public string ErrorStr = "";
    }

    public class SetPortalContactLoginDateResult
    {
        public bool IsError;
        public string ErrorStr = "";
    }

    public class CreateECRResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public int ErrorId = 0;
        public string WarningStr = "";
        public int EngagementId;
    }

    public class SetContactLoginDateRequest
    {
        public string Username;
        public string Password;
        public int ClientId;
        public string SessionId;
        public string CallerEmail;
        public string Email;
    }

    public class CreateECRRequest
    {
        public string AffiliateId;
        public string Username;
        public string Password;
        public int ClientId;
        public string ClientName;
        public string TrustIndexSurveyType;
        public string CountryCode;
        public int TotalEmployees;
        public string Email;
        public string FirstName;
        public string LastName;
        public string SessionId;
        public string CallerEmail;
    }
    public class createECRCountries
    {
        public string countryCode;
        public int totalEmployees;
    }

    public class GetEngagementInfoResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public bool FoundActiveEngagement;
        public EngagementInfo EInfo = new EngagementInfo();
        public CurrentCertificationInfo curCertInfo = new CurrentCertificationInfo();
    }

    public class EngagementInfo
    {
        public string ClientId = "";
        public int EngagementId = -1;
        public string CertificationStatus = "";
        public string ClientName = "";
        public string Tier = "";
        public string JourneyStatus = "";
        public string TrustIndexSSOLink = "";
        public string TrustIndexStatus = "";
        public string TrustIndexSourceSystemSurveyId = "";
        public string CultureAuditSSOLink = "";
        public string CultureAuditStatus = "";
        public string CultureAuditSourceSystemSurveyId = "";
        public string CultureBriefSSOLink = "";
        public string CultureBriefStatus = "";
        public string CultureBriefSourceSystemSurveyId = "";
        public string CompanyName = "";
        public string ReportDelivery = "";
        public string ReviewCenterPublishedLink = "";
        public string BestCompDeadline = "";
        public bool IsLatestECR = false;
        public bool IsAbandoned = false;
        public string AffiliateId = "";
        public bool isMNCCID = false;
        public string ECRCreationDate = "";
        public string ECRCountries = "";
        public int CountriesCount = 0;
    }

    public class ListDeadlineInfo
    {
        public string listDeadlineDate = "";
        public string listName = "";
    }

    public class GetListDeadlineInfoResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public ListDeadlineInfo ListDeadlineInfo = new ListDeadlineInfo();
    }

    public class AppConfigDetailsResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public string IsAppMaintenanceMode = "";
        public string AppVersion = "";
    }

    public class PostUserEventResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public UserEventEnums.Name userEventEnumName;
    }

    public class CurrentCertificationInfo
    {
        public int engagementId;
        public bool continueToShowEmpAndReports = false;
        public bool currentlyCertified = false;
        public string certificationStartDate = "";
        public string certificationExpiryDate = "";
        public string empResultsDate = "";
        public string reportDownloadsDate = "";
        public string trustIndexSSOLink = "";
        public string profilePublishedLink = "";
    }

    public class GetClientRecognitionInfoRequest
    {
        public string token;
        public int clientid;
    }

    public class GetClientRecognitionInfoResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public List<ClientRecognitionInfoDetail> ClientRecognitionInfoDetails = new List<ClientRecognitionInfoDetail>();
    }

    public class ClientRecognitionInfoDetail
    {
        public int yearly_list_id = -1;
        public string list_name = "";
        public int list_year = -1;
        public int rank = -1;
        public DateTime publication_date = DateTime.MinValue;
        public DateTime dashboard_date = DateTime.MinValue;
        public string list_logo_link = "";
        public bool toolkit_is_static = false;
        public string toolkit_custom_content = "";
        public string list_url = "";
    }

    public class GetCertificationToolkitRequest
    {
        public string token;
        public int YearlyListId;
    }

    public class GetCertificationToolkitResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public string ToolkitContent;
    }
    public class EmailToolkitResult
    {
        public bool IsError;
        public string ErrorStr = "";
    }
    public class GetHelpDeskLoginURLResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public string URL;
    }

    public class ListCalendarResult
    {
        public bool IsError { get; set; }
        public string ErrorStr { get; set; }

        public List<ListCalendar> ListCalendar = new List<ListCalendar>();


    }
    public class ListCalendar
    {
        public string Name { get; set; }
        public string Certified_by { get; set; }

        public string Publish_up { get; set; }
        public string url { get; set; }

    }
    public class GetCompanyUsersRequest
    {
        public string Username;
        public string Password;
        public int ClientId;
        public string SessionId;
        public string CallerEmail;
    }

    public class GetCompanyUsersResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public List<PortalContact> PortalContacts = new List<PortalContact>();
    }

    public class CreateUpdateUserRequest
    {
        public string Username;
        public string Password;
        public int ClientId;
        public string FirstName;
        public string LastName;
        public string Email;
        public bool AchievementNotification;
        public string SessionId;
        public string CallerEmail;
    }

    public class DeleteUserRequest
    {
        public string Username;
        public string Password;
        public int ClientId;
        public string Email;
        public string SessionId;
        public string CallerEmail;
    }

    public class GenericResult
    {
        public bool IsError;
        public string ErrorStr = "";
    }

    public class AddUpdateContactResult
    {
        public bool IsError;
        public int ErrorId;
        public string ErrorStr = "";
    }

    public class PortalContact
    {
        public string FirstName = "";
        public string LastName = "";
        public string Email = "";
        public bool AchievementNotification = false;
        public bool HasTenantIdClaim;
    }

    public class GetClientImageInfoResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public string SurveyId = "";
        public string SurveyStatus = "";
        public List<ClientPhoto> ClientPhotos = new List<ClientPhoto>();
        public string LogoFileName;
        public int EngagementId;
    }

    public class DeleteClientImageRequest
    {
        public string CultureSurveyId;
        public string uri;
    }

    public class DeleteClientImageResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
    }

    public class SaveClientImagesRequest
    {
        public string CultureSurveyId;
        public List<ClientPhoto> clientPhotos;
        public string LogoFileName;
    }

    public class SaveClientImagesResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
    }

    public class RepublishProfileResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public string ReviewPublishStatus = "";
    }

    public class GetNewECRCountriesResult
    {
        public bool Success = false;
        public string ErrorMessage = "";
        public List<Country> Countries { get; set; }
    }

    public class GeneralCallResult
    {
        public bool Success = false;
        public string ErrorMessage = "";
    }

    public class RepublishProfileRequest
    {
        public string Username;
        public string Password;
        public int ClientId;
        public int EngagementId;
    }

    public class SetCustomerActivationStatusRequest
    {
        public string Username;
        public string Password;
        public int ClientId;
        public int EngagementId;
        public EngagementUpdateField EngagementUpdateField;
    }

    public class SetCustomerActivationStatusResult
    {
        public bool IsError;
        public string ErrorStr = "";
    }

    public class ClaimResult
    {
        public string value;
        public bool isSuccess;
    }

    public class GetDashboardDataResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public List<ECRV2Info> ECRV2s = new List<ECRV2Info>();
    }

    public class ECRV2Info
    {
        public string id;
        public int cid = 0;
        public int eid = 0;
        public string createdate;
        public string cname;
        public string tistatus;
        public string cbstatus;
        public string castatus;
        public string cstatus;
        public string tools;
        public string tilink;
        public string cblink;
        public string calink;
        public string rstatus;
        public string rlink;
        public string lstatus;
        public string certexdate;
        public string country;
        public string tier;
        public string journeystatus;
        public string journeyhealth;
        public int duration = 0;
        public string renewalstatus;
        public string renewalhealth;
        public string engagementstatus;
        public string engagementhealth;
        public int numberofsurveyrespondents = 0;
        public Boolean abandoned = false;
        public Boolean favorite = false;
        public string surveyopendate;
        public string surveyclosedate;
        public string allcountrycertification;
        public string allcountrylisteligiblity;
    }

    public class SetStatusResult
    {
        public bool IsError = true;
        public string ErrorStr = "";
    }

    public class OptOutAbandonSurveyResult
    {
        public bool IsError = true;
        public string ErrorStr = "";
    }

    public class SetRenewalStatusResult
    {
        public bool IsError = true;
        public string ErrorStr = "";
    }

    public class GetAffiliateResult
    {
        public bool IsError = true;
        public string ErrorStr = "";
        public string AffiliateId = "";
    }

    public class FindMostRecentCertificationEIDResult
    {
        public bool ErrorOccurred;
        public string ErrorMessage = "";
        public bool FoundCertificationEngagement = false;
        public int EngagementId = -1;
        public string CertDate = "";
        public string CertExpiryDate = "";
        public string CertCountryCode = "";
        public string AffiliateId = "";
    }

    public class AtlasCallSetStatusRequest
    {
        public string Username;
        public string Password;
        public string ClientId;
        public string EngagementId;
        public string Cert;
        public string List;
        public string Renewal;
        public string CallerEmail;
        public string SessionId;
    }

    public class AtlasCallSetStatusResult
    {
        public bool IsError;
        public string ErrorStr = "";
    }

    public class AtlasToolKitEmailContentCallResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public string EmailContent = "";
    }

    public class GetAffiliateIdByClientIdResult
    {

    }

    public class MultiplePackageDownload
    {
        public string Token { get; set; }
        public bool IsDownloadCA { get; set; }
        public bool IsDownloadCB { get; set; }
        public bool IsDownloadDataExtract { get; set; }
        public string[] SelectedCountryCodes { get; set; }
        public int ClientId { get; set; }
        public int EngagementId { get; set; }
        public string CountryCode { get; set; }
        public string Email { get; set; }
        public string AffiliateId { get; set; }
    }

    public class SubmitDataRequestResult
    {
        public bool IsError;
        public string ErrorStr = "";
    }

    public class GetCountriesForDataRequestResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public List<Country> Countries { get; set; }
    }

    public class GetDataExtractRequestsResult
    {
        public bool IsError;
        public string ErrorStr = "";
        public List<DataExtractRequest> DataExtractRequests = new List<DataExtractRequest>();
    }

    public class DataExtractRequest
    {
        public string Id;
        public DateTime DateRequested;
        public string Status = "";
        public string Link = "";
        public string AffiliateId = "";
        public string Requestor = "";
    }

    public class CreateEcrRequest2
    {
        public string AffiliateId;
        public string Username;
        public string Password;
        public int ClientId;
        public string ClientName;
        public string TrustIndexSurveyType;
        public string CountryCode;
        public int TotalEmployees;
        public string Email;
        public string FirstName;
        public string LastName;
        public string SessionId;
        public string CallerEmail;
        public bool IsCreateCA;
        public SurveyCountry[] SurveyCountries;
    }

    public class SurveyCountry
    {
        public string CountryCode;
        public int TotalEmployees;
        public bool IsApplyForCertification;
    }

    public class GetDataExtractCompanyInfoByClientIdResult
    {
        public bool IsSuccess;
        public string ErrorMessage = "";
        public List<DataExtractCompanyInfo> DataExtractCompanyInfos = new List<DataExtractCompanyInfo>();
    }

    public class DataExtractCompanyInfo
    {
        public int ClientId;
        public int EngagementId;
        public string ClientName;
    }

    public enum TableOfContentStatus
    {
        Hidden,
        In_Progress,
        Incomplete,
        Complete,
        Not_Started
    }

    public enum TableOfContentSection
    {
        Hidden,
        OpenApplication,
        SubmittedApplication
    }
}
