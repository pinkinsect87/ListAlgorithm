using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SharedProject2;
using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using Microsoft.Azure.Storage.Blob;
using System.Text;
using System.Net;
using Microsoft.Azure.Storage;
using System.Text.RegularExpressions;

//Client users should have an "idp" claim of "local"
//and a "role" claim of "ClientAppRole_PortalManager"
//and a "gptw_client_id" claim of "?????"

//Employees should have an idp claim of "GptwEmployeeLogin"
//and a "GptwAd_GptwAppRole_PortalGeneral" claim of "GptwAppRole_PortalGeneral"


namespace Portal.Controllers
{
    public class PortalControllerBase : ControllerBase
    {
        private List<Claim> _claims = null;
        private AppSettings _appSettings = null;
        private TelemetryClient _appInsights = null;
        private GptwLog _atlasLog = null;
        private IHttpClientFactory _clientFactory;
        private string _tokenFromExternalSource = null;
        private AtlasOperationsDataRepository _AORepository = null;
        private CultureSurveyDataRepository _CSRepository = null;
        private static string defaultMIMEType = "application/octet-stream";

        // use constructor injection to get a TelemetryClient instance. no more singletons to get Application Insights access for us.
        public PortalControllerBase(TelemetryClient telemetry, IOptions<AppSettings> appsettings, IHttpClientFactory clientFactory)
        {
            _appInsights = telemetry;
            _appSettings = appsettings.Value;

            GptwLogConfigInfo logConfigInfo = new GptwLogConfigInfo("Portal", appSettings.Environment, appSettings.MongoDBConnectionString,
                appSettings.ServiceBusLogConnectionString, telemetry);
            _atlasLog = new GptwLog(logConfigInfo);

            _clientFactory = clientFactory;
        }

        public AtlasOperationsDataRepository AORepository
        {
            get
            {
                if (this._AORepository == null)
                    this._AORepository = new AtlasOperationsDataRepository(appSettings.MongoDBConnectionString, appSettings.MongoLockAzureStorageConnectionString);
                return _AORepository;
            }
        }

        public CultureSurveyDataRepository CSRepository
        {
            get
            {
                if (this._CSRepository == null)
                    this._CSRepository = new CultureSurveyDataRepository(appSettings.MongoDBConnectionString, appSettings.MongoLockAzureStorageConnectionString);
                return _CSRepository;
            }
        }

        public IHttpClientFactory clientFactory
        {
            get { return _clientFactory; }
        }

        public AppSettings appSettings
        {
            get { return _appSettings; }
        }

        public GptwLog AtlasLog
        {
            get { return _atlasLog; }
        }

        public TelemetryClient appInsights
        {
            get { return _appInsights; }
        }

        public string authorizationToken
        {
            get { return Request.Headers.FirstOrDefault(h => h.Key.Equals("Authorization")).Value; }
        }

        public void SetTokenFromExternalSource(String token)
        {
            _tokenFromExternalSource = token;
        }

        public void ClearTokenFromExternalSource(String token)
        {
            _tokenFromExternalSource = null;
        }

        public string TokenFromExternalSource
        {
            get { return _tokenFromExternalSource; }
        }

        public GptwLogContext GetNewGptwLogContext(string methodName, GptwLogContext parentContext = null)
        {
            GptwLogContext gptwLogContext = new GptwLogContext()
            {
                SessionId = Request.Headers["portal-session-id"].ToString()
            };

            if (String.IsNullOrEmpty(gptwLogContext.SessionId))
                gptwLogContext.SessionId = Guid.NewGuid().ToString();

            if (parentContext == null)
            {
                SetUpContextInfoFromClaims(gptwLogContext);
            }
            else
            {
                gptwLogContext.ClientId = parentContext.ClientId;
                gptwLogContext.EngagementId = parentContext.EngagementId;
                gptwLogContext.Email = parentContext.Email;
            }

            gptwLogContext.MethodName = methodName;

            if (!methodName.Equals("GetAppConfigDetails", StringComparison.OrdinalIgnoreCase))
                AtlasLog.LogInformation("called.", gptwLogContext);

            return gptwLogContext;
        }

        // Called by GetListDeadlineInfo, GetListCalendar and getNextListCertification which don't involve
        // a cid and therefore don't contain any information that is proprietory to any affiliate
        public Boolean IsUserAuthorizedNotClientIdSpecific(GptwLogContext gptwContext)
        {
            try
            {
                GetAllClaims(gptwContext);

                // Note: This is the only method which is allowed to call IsAuthorizedEmployee without either passing a cid or an affiliateId.
                // and this is only because the calls that use this method don't have a cid and that data isn't proprietary to any specific affiliateId
                if (IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext))
                    return true;

                // verify that they are a client type user by having an idp of local
                Claim idpClaim = (from c in _claims.AsQueryable()
                                  where String.Equals(c.ClaimType, "idp", StringComparison.OrdinalIgnoreCase) &&
                                      String.Equals(c.ClaimValue, "local", StringComparison.OrdinalIgnoreCase)
                                  select c).SingleOrDefault();

                if (idpClaim == null)
                {
                    AtlasLog.LogError(String.Format("IsUserAuthorizedNotClientIdSpecific- Unexpected event. Not an employee and yet no idp:local claim. Disallowing access."), gptwContext);
                    return false;
                }

                // verify that the client type user has a ClientAppRole_PortalManager
                Claim portalClaim = (from c in _claims.AsQueryable()
                                     where String.Equals(c.ClaimType, "role", StringComparison.OrdinalIgnoreCase) &&
                                         String.Equals(c.ClaimValue, "ClientAppRole_PortalManager", StringComparison.OrdinalIgnoreCase)
                                     select c).SingleOrDefault();

                if (portalClaim == null)
                {
                    AtlasLog.LogError(String.Format("IsUserAuthorizedNotClientIdSpecific- Unexpected event. Not an employee, idp:local claim found however no role of ClientAppRole_PortalManager found. Disallowing access."), gptwContext);
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("IsUserAuthorizedNotClientIdSpecific Exception caught."), e, gptwContext);
            }
            return false;
        }

        // Return a false (with an empty string) if unsuccesfull in retrieving the affiliateId
        private Boolean GetAffiliateIdAssociatedWithClientId(int clientId, out string affiliateId)
        {
            affiliateId = "";

            affiliateId = this.AORepository.RetrieveAffiliateIdByClientId(clientId);

            return (!String.IsNullOrEmpty(affiliateId));
        }

        //NOTE: this is called in those 3-4 situations where there isn't a cid available so we can't verify the affiliate.
        // Zendesk, document and image upload and save
        public Boolean IsAuthorizedEmployeeNoCIDAFFCheck(GptwLogContext gptwContext)
        {
            try
            {
                GetAllClaims(gptwContext);

                Claim idpClaim = (from c in _claims.AsQueryable()
                                  where String.Equals(c.ClaimType, "idp", StringComparison.OrdinalIgnoreCase) &&
                                      String.Equals(c.ClaimValue, "GptwEmployeeLogin", StringComparison.OrdinalIgnoreCase)
                                  select c).SingleOrDefault();

                if (idpClaim == null)
                    return false;

                Claim portalClaim = (from c in _claims.AsQueryable()
                                     where String.Equals(c.ClaimType, "GptwAd_GptwAppRole_PortalGeneral", StringComparison.OrdinalIgnoreCase) &&
                                         String.Equals(c.ClaimValue, "GptwAppRole_PortalGeneral", StringComparison.OrdinalIgnoreCase)
                                     select c).SingleOrDefault();

                if (portalClaim == null)
                    return false;

                //name:GptwAd_GptwAppEnvironment_PROD value:GptwAppEnvironment_PROD
                Claim environmentClaim = (from c in _claims.AsQueryable()
                                          where String.Equals(c.ClaimType, "GptwAd_" + appSettings.ExpectedEnvironmentClaim, StringComparison.OrdinalIgnoreCase) &&
                                              String.Equals(c.ClaimValue, appSettings.ExpectedEnvironmentClaim, StringComparison.OrdinalIgnoreCase)
                                          select c).SingleOrDefault();

                return environmentClaim != null;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("IsAuthorizedEmployeeNoCIDAFFCheck Exception caught."), e, gptwContext);
            }
            return false;
        }

        public Boolean IsUserAuthorized(GptwLogContext gptwContext, int clientIdBeingAccessed)
        {
            try
            {
                // Only employees need to passed down this path
                if (IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext))
                {
                    // IsAuthorizedEmployee will GetAllClaims
                    if (IsAuthorizedEmployee(gptwContext, clientIdBeingAccessed))
                        return true;

                    return false;
                }

                // verify that they are a client type user by having an idp of local
                Claim idpClaim = (from c in _claims.AsQueryable()
                                  where String.Equals(c.ClaimType, "idp", StringComparison.OrdinalIgnoreCase) &&
                                      (String.Equals(c.ClaimValue, "local", StringComparison.OrdinalIgnoreCase) ||
                                      String.Equals(c.ClaimValue, "UkgSamlLogin", StringComparison.OrdinalIgnoreCase))
                                  select c).SingleOrDefault();

                if (idpClaim == null)
                {
                    AtlasLog.LogError(String.Format("IsUserAuthorized- Unexpected event. Not an employee. No idp:local claim found. Disallowing access."), gptwContext);
                    return false;
                }

                // verify that the client type user has a ClientAppRole_PortalManager
                Claim portalClaim = (from c in _claims.AsQueryable()
                                     where String.Equals(c.ClaimType, "role", StringComparison.OrdinalIgnoreCase) &&
                                         String.Equals(c.ClaimValue, "ClientAppRole_PortalManager", StringComparison.OrdinalIgnoreCase)
                                     select c).SingleOrDefault();

                if (portalClaim == null)
                {
                    AtlasLog.LogError(String.Format("IsUserAuthorized- Unexpected event. Not an employee. An idp:local claim  was found. No role of ClientAppRole_PortalManager found. Disallowing access."), gptwContext);
                    return false;
                }

                // Now verify that the have gptw_client_id claim
                Claim clientIdClaim = (from c in _claims.AsQueryable()
                                       where String.Equals(c.ClaimType, "gptw_client_id", StringComparison.OrdinalIgnoreCase)
                                       select c).SingleOrDefault();

                if (clientIdClaim == null)
                {
                    AtlasLog.LogError(String.Format("IsUserAuthorized- Unexpected event. Not an employee. An idp:local claim was found and a role of ClientAppRole_PortalManager was found however no clientIdClaim was found. Disallowing access."), gptwContext);
                    return false;
                }

                // And that the gptw_client_id claim matches the one that they are attempting to access
                int clientIdFromClaims = Int32.Parse(clientIdClaim.ClaimValue);

                if (clientIdFromClaims != clientIdBeingAccessed)
                {
                    AtlasLog.LogError(String.Format("IsUserAuthorized- Unexpected event. Not an employee. An idp:local claim, a role of ClientAppRole_PortalManager and a gptw_client_id claim was found however the user was attempting to access {0} when they had a gptw_client_id claim of {1}. Disallowing access.", clientIdBeingAccessed, clientIdClaim.ClaimValue), gptwContext);
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("IsUserAuthorized Exception caught."), e, gptwContext);
            }
            return false;
        }

        public Boolean IsAuthorizedEmployee(GptwLogContext gptwContext, int clientId)
        {
            try
            {
                string affiliateId = "";

                if (!GetAffiliateIdAssociatedWithClientId(clientId, out affiliateId))
                {
                    AtlasLog.LogError(String.Format("IsAuthorizedEmployee(clientId)- GetAffiliateIdAssociatedWithClientId failed to return an affiliateId for cid:{0}", clientId), gptwContext);
                    return false;
                }

                return IsAuthorizedEmployee(gptwContext, affiliateId);
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("IsAuthorizedEmployee(clientId) Exception caught."), e, gptwContext);
            }
            return false;
        }

        public Boolean IsAuthorizedEmployee(GptwLogContext gptwContext, string affiliateId)
        {
            try
            {
                GetAllClaims(gptwContext);

                // Check for AffiliateId first
                string affiliateClaim = string.Format("GptwAd_GptwAffiliateId_{0}", affiliateId);

                Claim foundClaim = (from c in _claims.AsQueryable()
                                    where String.Equals(c.ClaimType, affiliateClaim)
                                    select c).SingleOrDefault();

                if (foundClaim == null)
                {
                    AtlasLog.LogError(String.Format("IsAuthorizedEmployee(affiliateId). Employee does not have affiliate claim. token:{0}, affiliateId:{1}, email:{2}", authorizationToken, affiliateId, gptwContext.Email), gptwContext);
                    return false;
                }

                return IsAuthorizedEmployeeNoCIDAFFCheck(gptwContext);
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("IsAuthorizedEmployee(affiliateId) Exception caught."), e, gptwContext);
            }
            return false;
        }

        // Not used internally
        // Used for user type (enduser or employee) when posting KPI's. Also used in Zendesklogin
        public Boolean IsGPTWEmployee(GptwLogContext gptwContext)
        {
            try
            {
                GetAllClaims(gptwContext);

                Claim idpClaim = (from c in _claims.AsQueryable()
                                  where String.Equals(c.ClaimType, "idp", StringComparison.OrdinalIgnoreCase) &&
                                      String.Equals(c.ClaimValue, "GptwEmployeeLogin", StringComparison.OrdinalIgnoreCase)
                                  select c).SingleOrDefault();

                return idpClaim != null;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("IsGPTWEmployee Exception caught."), e, gptwContext);
            }
            return false;
        }

        // return false if failure. true if successfull in returning a value.
        public ClaimResult GetClaim(GptwLogContext gptwContext, string claimType)
        {
            ClaimResult result = new ClaimResult
            {
                isSuccess = false,
                value = ""
            };

            GetAllClaims(gptwContext);

            result.value = (from claim in _claims
                            where claim.ClaimType == claimType
                            select claim.ClaimValue).SingleOrDefault();

            result.isSuccess = result.value != null;

            return result;
        }

        // Allow the logging of claim information to be skipped the first time through which is for context logging of the method name only
        private void GetAllClaims(GptwLogContext gptwContext, bool logClaimInfo = true)
        {
            string logInfo = "";
            try
            {
                if (_claims == null)
                {
                    //AtlasLog.LogInformation(String.Format("GetAllClaims User.Claims: {0}.", User.Claims));

                    _claims = (from c in User.Claims select (new Claim { ClaimType = c.Type, ClaimValue = c.Value })).ToList();

                    //foreach (var claim in _claims)
                    //    AtlasLog.LogInformation(String.Format("GetAllClaims returned  {0}/{1}.", claim.ClaimType, claim.ClaimValue));

                    // This is for the case where we passed the token up via the get (as opposed to the Authorization Request Header)
                    if (_claims.Count == 0 && TokenFromExternalSource != null)
                    {
                        try
                        {
                            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                            JwtSecurityToken jwtST = tokenHandler.ReadJwtToken(TokenFromExternalSource);
                            _claims = (from c in jwtST.Claims select (new Claim { ClaimType = c.Type, ClaimValue = c.Value })).ToList();
                        }
                        catch (Exception e)
                        {
                            logInfo += String.Format("ReadJwtToken failed with exception:{0}", e.Message);
                        }
                    }

                    foreach (var claim in _claims)
                        logInfo += "," + claim.ClaimType + ":" + claim.ClaimValue;

                    HttpResponseMessage response;

                    using (var client = _clientFactory.CreateClient())
                    {
                        string _token = TokenFromExternalSource;
                        if (_token == null)
                        {
                            _token = Request.Headers.FirstOrDefault(h => h.Key.Equals("Authorization")).Value;
                            _token = _token.Replace("Bearer ", ""); // Remove Bearer from token
                        }
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                        logInfo += ", token:" + _token;
                        response = client.GetAsync(appSettings.AuthServerUserInfoEndpoint).Result;

                    }

                    logInfo += String.Format(", returned from userinfo call- response.statusCode:{0}", response.StatusCode);

                    //AtlasLog.LogInformation(String.Format(", returned from userinfo call- response.statusCode:{0}", response.StatusCode));

                    if (!response.IsSuccessStatusCode)
                    {
                        AtlasLog.LogError(String.Format("GetAllClaims returned an http status of {0}.", response.StatusCode), gptwContext);
                        return;
                    }

                    try
                    {
                        string responseString = response.Content.ReadAsStringAsync().Result;

                        Dictionary<string, Object> dict = JsonConvert.DeserializeObject<Dictionary<string, Object>>(responseString);

                        String returnedClaims = "";
                        foreach (var claim in dict)
                        {

                            returnedClaims += "," + claim.Key + ":";


                            //AtlasLog.LogInformation(String.Format("userinfo: claim.Key:{0},claim.Value{1}", claim.Key, claim.Value));


                            //  if it's not one of these that we want to suppress the value
                            if (String.Compare(claim.Key, "given_name", true) == 0 || String.Compare(claim.Key, "family_name", true) == 0
                                || String.Compare(claim.Key, "name", true) == 0)
                            {
                                returnedClaims += "REDACTED";
                            }
                            else
                            {
                                returnedClaims += claim.Value;
                            }

                            if (claim.Value is String)
                            {
                                _claims.Add(new Claim { ClaimType = claim.Key, ClaimValue = claim.Value.ToString() });
                            }
                            else
                            {
                                String valueAsString = claim.Value.ToString();
                                List<string> listOfClaims = JsonConvert.DeserializeObject<List<string>>(valueAsString);

                                foreach (var value in listOfClaims)
                                {
                                    _claims.Add(new Claim { ClaimType = claim.Key, ClaimValue = value });
                                }
                            }
                        }

                        logInfo += String.Format(", userinfo claims:{0}", returnedClaims);
                        if (logClaimInfo)
                            AtlasLog.LogInformation(String.Format("GetAllClaims:{0}", logInfo), gptwContext);
                    }
                    catch (Exception e)
                    {
                        AtlasLog.LogInformation(String.Format("GetAllClaims:{0}", logInfo));
                        AtlasLog.LogErrorWithException(String.Format("GetAllClaims parsing failure Exception caught."), e, gptwContext);
                    }

                }
            }
            catch (Exception e)
            {
                AtlasLog.LogInformation(String.Format("GetAllClaims:{0}", logInfo));
                AtlasLog.LogErrorWithException(String.Format("GetAllClaims Exception caught."), e, gptwContext);
            }
        }

        private void SetUpContextInfoFromClaims(GptwLogContext gptwContext)
        {
            try
            {
                GetAllClaims(gptwContext, true);

                Claim idpClaim = (from c in _claims.AsQueryable()
                                  where String.Equals(c.ClaimType, "idp", StringComparison.OrdinalIgnoreCase) &&
                                      String.Equals(c.ClaimValue, "GptwEmployeeLogin", StringComparison.OrdinalIgnoreCase)
                                  select c).SingleOrDefault();

                if (idpClaim != null)
                {
                    string employeeEmailClaimValue = (from claim in _claims
                                                      where claim.ClaimType == "upn"
                                                      select claim.ClaimValue).SingleOrDefault();

                    if (employeeEmailClaimValue != null)
                    {
                        gptwContext.Email = employeeEmailClaimValue;
                    }
                }
                else
                {
                    string endUserEmailClaimValue = (from claim in _claims
                                                     where claim.ClaimType == "email"
                                                     select claim.ClaimValue).SingleOrDefault();

                    if (endUserEmailClaimValue != null)
                    {
                        gptwContext.Email = endUserEmailClaimValue;
                    }

                    Claim clientIdClaim = (from c in _claims.AsQueryable()
                                           where String.Equals(c.ClaimType, "gptw_client_id", StringComparison.OrdinalIgnoreCase)
                                           select c).SingleOrDefault();

                    if (clientIdClaim != null)
                    {
                        gptwContext.ClientId = Int32.Parse(clientIdClaim.ClaimValue);
                    }
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("SetUpContextInfoFromClaims Exception caught."), e, gptwContext);
            }
        }

        /// <summary>
        /// Test if the given filename contains any invalid filename characters
        /// </summary>
        /// <param name="fileName">Filename to be tested</param>
        /// <returns>True if the filename contains invalid characters</returns>
        public bool HasInvalidFileNameChars(string fileName)
        {
            bool hasInvalidChars = false;

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (fileName.Contains(c))
                {
                    hasInvalidChars = true;
                    break;
                }
            }
            return hasInvalidChars;
        }

        public bool ValidateFile(string fileName, Stream fs, string cid)
        {
            bool isValid = false;

            // Upload the file to a temporary storage container
            var isUploadSuccessful = UploadFileToContainer(fileName, fs, cid);

            // If file uploaded, then ready to validate
            if (isUploadSuccessful)
                isValid = CallFileValidator(fileName, cid);

            return isValid;
        }

        // Upload a fileStream to an Azure storage container
        private bool UploadFileToContainer(string fileName, Stream fs, string cid)
        {
            bool isUploadSuccessful;

            try
            {
                string uri = Regex.Replace(appSettings.FileValidationBlobSASURI, @"(\s|&amp;)", "&");
                var container = new CloudBlobContainer(new Uri(uri, UriKind.Absolute));

                CloudBlockBlob blob = container.GetBlockBlobReference(fileName);
                string fileExtension = getExtension(fileName);
                blob.Properties.ContentType = ToMIMEType(fileExtension);
                blob.Metadata.Add("FileName", fileName);
                blob.UploadFromStream(fs);
                isUploadSuccessful = true;
            }
            catch (Exception ex)
            {
                isUploadSuccessful = false;
                AtlasLog.LogError(String.Format("UploadFileToContainer exception thrown. cid:{0}, errorMessage:{1}", cid, ex.Message));
            }

            return isUploadSuccessful;
        }

        private bool CallFileValidator(string fileName, string cid)
        {
            bool isValid = false;

            // Build the content
            var pst = new { Path = fileName };
            string outputJson = JsonConvert.SerializeObject(pst);
            StringContent stuffToPost = new StringContent(outputJson);
            stuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpClient hc = new HttpClient();

            string encodedAuthorizationHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", appSettings.FileValidationClientId, _appSettings.FileValidationWebApiSecret)));
            // hc.DefaultRequestHeaders.Add("Authorization", "Basic " & encodedAutorizationHeader)
            hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Basic.ToString(), encodedAuthorizationHeader);

            try
            {
                HttpResponseMessage resp = hc.PostAsync(this._appSettings.FileValidationWebApiUrl, stuffToPost).Result;
                if (resp.IsSuccessStatusCode)
                {
                    string resultStr = resp.Content.ReadAsStringAsync().Result;
                    var validationResult = JsonConvert.DeserializeObject<ValidationResultMessage>(resultStr);
                    isValid = validationResult.IsValid;
                    if (!isValid)
                    {
                        AtlasLog.LogError(String.Format("FileValidation failed for. cid:{0}, filename:{1}", cid, fileName));
                    }
                }
                else
                {
                    AtlasLog.LogError(String.Format("CallFileValidator failed. cid:{0}, httpStatusCode:{1}, resp.ReasonPhrase:{2}", cid, resp.StatusCode, resp.ReasonPhrase));
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                AtlasLog.LogError(String.Format("CallFileValidator exception thrown. cid:{0}, errorMessage:{1}", cid, ex.Message));
            }

            return isValid;
        }

        private string getExtension(string filename)
        {
            var dotExtension = filename.LastIndexOf(".");
            string extension = filename.Substring(dotExtension + 1);
            return extension;
        }

        private IDictionary<string, string> extensionMIMETypeMapping = new Dictionary<string, string>()
        {
            {
                "txt",
                "text/plain"
            },
            {
                "rtf",
                "text/richtext"
            },
            {
                "wav",
                "audio/wav"
            },
            {
                "gif",
                "image/gif"
            },
            {
                "jpeg",
                "image/jpeg"
            },
            {
                "png",
                "image/png"
            },
            {
                "tiff",
                "image/tiff"
            },
            {
                "bmp",
                "image/bmp"
            },
            {
                "avi",
                "video/avi"
            },
            {
                "mpeg",
                "video/mpeg"
            },
            {
                "pdf",
                "application/pdf"
            },
            {
                "doc",
                "application/msword"
            },
            {
                "dot",
                "application/msword"
            },
            {
                "docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            },
            {
                "dotx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.tempate"
            },
            {
                "xls",
                "application/vnd.ms-excel"
            },
            {
                "xlt",
                "application/vnd.ms-excel"
            },
            {
                "csv",
                "application/vnd.ms-excel"
            },
            {
                "xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            },
            {
                "xltx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.template"
            },
            {
                "ppt",
                "application/vnd.ms-powerpoint"
            },
            {
                "pot",
                "application/vnd.ms-powerpoint"
            },
            {
                "pptx",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            },
            {
                "potx",
                "application/vnd.openxmlformats-officedocument.presentationml.template"
            }
        };

        private string ToMIMEType(string extension)
        {
            if (extension == null || extension.Length == 0)
                return defaultMIMEType;
            string lowerExtension = extension.ToLower();
            string mime = "";
            if (!extensionMIMETypeMapping.TryGetValue(lowerExtension, out mime))
                mime = defaultMIMEType;
            return mime;
        }

        private string UnicodeToBase64(string sText)
        {
            byte[] toEncodeAsBytes = System.Text.UnicodeEncoding.Unicode.GetBytes(sText);
            return System.Convert.ToBase64String(toEncodeAsBytes);
        }

        public class ValidationResultMessage
        {
            public bool IsValid { get; set; }
            public string FilePath { get; set; }
            public string Metadata { get; set; }
            public bool ValidationCompletedSuccessfully { get; set; }
        }

        public bool SetCustomerActivationStatus(int engagementId, EngagementUpdateField field)
        {
            bool success = false;
        
            try {
                ECRV2 ecrv2 = this.AORepository.RetrieveReadWriteECRV2(engagementId);
                switch (field)
                {
                    case EngagementUpdateField.CustomerActivationEmprisingAccess:
                        ecrv2.CustomerActivationEmprisingAccess = ActivationStatus.YES;
                        break;
                    case EngagementUpdateField.CustomerActivationBadgeDownload:
                        ecrv2.CustomerActivationBadgeDownload = ActivationStatus.YES;
                        break;
                    case EngagementUpdateField.CustomerActivationShareToolkit:
                        ecrv2.CustomerActivationShareToolkit = ActivationStatus.YES;
                        break;
                    case EngagementUpdateField.CustomerActivationSharableImages:
                        ecrv2.CustomerActivationSharableImages = ActivationStatus.YES;
                        break;
                    case EngagementUpdateField.CustomerActivationReportDownload:
                        ecrv2.CustomerActivationReportDownload = ActivationStatus.YES;
                        break;
                    default:
                        break;
                }
                this.AORepository.SaveECRV2(ref ecrv2);
                success = true;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("SetCustomerActivationStatus."), e);
                success = false;
            }

            return success;
        }
    }
}
