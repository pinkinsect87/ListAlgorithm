using System;
using Microsoft.AspNetCore.Mvc;
using Portal.Model;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Aspose.Svg;
using Aspose.Svg.Rendering.Image;
using SharedProject2;
using System.IO.Compression;
using System.Collections.Generic;
using Newtonsoft.Json;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Reflection;
using System.Linq;
using System.Globalization;
using Portal.Misc;
using System.Xml.Linq;

namespace Portal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BadgeController : PortalControllerBase
    {
        public BadgeController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        [HttpGet("[Action]")]
        public IActionResult CreateBadge(int clientId, int engagementId, string imageType, int quality, string token)
        {
            this.SetTokenFromExternalSource(token);
            GptwLogContext gptwContext = GetNewGptwLogContext("CreateBadge", null);

            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = clientId;
                if (gptwContext.EngagementId <= 0)
                    gptwContext.EngagementId = engagementId;
            }
            catch (Exception) { }

            if (!IsUserAuthorized(gptwContext, clientId))
            {
                AtlasLog.LogError(String.Format("Not Authorized.token:{0}", token), gptwContext);
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                this.ClearTokenFromExternalSource(token);
                return Unauthorized();
            }

            // The token is no longer needed to be overidden at this point so clear it.
            this.ClearTokenFromExternalSource(token);

            //// *******************************************************
            //// Special entry point to create badges for all countries
            //// Instructions:
            //// 1. Uncomment these lines
            //// 2. Change the start and end certification dates as needed
            //// 3. Run the portal code in the debugger
            //// 4. When the portal dashboard comes up, choose any certified company
            //// This code will be hit on the first request for a badge
            //// After the badges have run, restore this file from source control
            //// *******************************************************
            //engagementId = -1;
            //if (engagementId == -1)
            //{
            //    DateTime startCertificationDate = new DateTime(2022, 1, 1);
            //    DateTime endCertificationDate = new DateTime(2023, 12, 1);
            //    CreateAllCountryBadges(startCertificationDate, endCertificationDate);
            //    return null;
            //}

            // This path produces a badge, in an svg format, with dynamically inserted content, and with no text vectorization. 
            try
            {

                BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(this.AORepository, gptwContext, AtlasLog);

                // Get the badge template to be modified
                BadgeContentHelpers.BadgeInfo badgeInfo = badgeContentHelpers.getBadgeInfo(engagementId, gptwContext);
                string badgeTemplate = badgeInfo.BadgeTemplate;

                // Gather the dynamic content    
                Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();
                try
                {
                    dynamicContent = badgeContentHelpers.gatherBadgeDynamicContent(engagementId, gptwContext);
                }
                catch (Exception e)
                {
                    AtlasLog.LogErrorWithException(String.Format("Unable to gather content for the shareable badge template '{0}'", badgeTemplate), e, gptwContext);
                    throw (new Exception(String.Format("Unable to gather content")));
                }

                // Create the badge as a stream
                ImageBuilder imageBuilder = new ImageBuilder(this.AORepository, gptwContext, AtlasLog);
                Stream outputStream = imageBuilder.CreateImageStream(imageType, badgeTemplate, dynamicContent);

                // Name to give the delivered file
                string downloadFilename = badgeContentHelpers.makeDownloadableFilename(engagementId, imageType);

                // Return the result
                string contentType = getContentType(imageType);
                FileStreamResult fs = File(outputStream, contentType, downloadFilename);

                AtlasLog.LogInformation(String.Format("Returning a badge of type '{0}'", imageType), gptwContext);

                return fs;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unable to render a badge"), e, gptwContext);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Create badges for all countries in each of the image formats supported
        /// </summary>
        /// <param name="startCertificationDate">Starting date to put on the badge</param>
        /// <param name="endCertificationDate">Ending date to put on the badge</param>
        public void CreateAllCountryBadges(DateTime startCertificationDate, DateTime endCertificationDate)
        {

            // TODO put this code into a controller so it can be called from a page instead of run locally
            // TODO write each file to a ZIP archive instead of a local folder

            GptwLogContext gptwContext = GetNewGptwLogContext("CreateAllCountryBadges", null);

            try
            {
                BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(this.AORepository, gptwContext, AtlasLog);

                Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

                // Get countries
                List<Country> allCountries = AORepository.GetAllCountries();

                List<string> imageTypes = new List<string> { "svg", "png", "jpg" };

                DateTime certificationDate = startCertificationDate;

                while (certificationDate <= endCertificationDate)
                {
                    // Iterate through each country
                    foreach (Country country in allCountries)
                    {
                        Console.WriteLine(String.Format(@"Building badges for {0}-{1} {2}", certificationDate.ToString("MMM"), certificationDate.ToString("yyyy"), country.CountryName));

                        // Iterate through each country badge language
                        foreach (BadgePreference badgePreference in country.BadgePreferences)
                        {
                            // Iterate through each image type
                            foreach (string imageType in imageTypes)
                            {
                                // Gather the dynamic content    
                                dynamicContent.Clear();
                                string badgeTemplate = "Portal.Resources." + badgePreference.BadgeTemplate;
                                string badgeCountry = badgePreference.ShortNameForBadge;
                                dynamicContent.Add("countryName", new KeyValuePair<string, string>(badgeCountry, "text"));
                                string badgeDateRange = badgeContentHelpers.buildBadgeDateRange(certificationDate, certificationDate.AddMonths(12), badgePreference.CultureId);
                                dynamicContent.Add("dateRange", new KeyValuePair<string, string>(badgeDateRange, "text"));

                                // Create the badge as a stream
                                ImageBuilder imageBuilder = new ImageBuilder(this.AORepository, gptwContext, AtlasLog);
                                Stream outputStream = imageBuilder.CreateImageStream(imageType, badgeTemplate, dynamicContent);

                                // Format the file and folder names
                                string countryName = country.CountryName;
                                string languageName = badgePreference.BadgeVersion;
                                string year = certificationDate.ToString("yyyy");
                                string month = certificationDate.ToString("MMM").ToUpper();
                                string monthNumber = certificationDate.ToString("MM");
                                string fileName = String.Format(@"{0}-{1}{2}-{3}.{4}", year, monthNumber, month, languageName, imageType);
                                string folder = String.Format(@"c:\temp\badges\{0}\{1}\{2}\{3}-{4}", countryName, languageName, year, monthNumber, month);
                                // Create the folder if it doesn't exist yet
                                if (!Directory.Exists(folder))
                                {
                                    Directory.CreateDirectory(folder);
                                }
                                
                                // Output the badge to the folder
                                string path = Path.Combine(folder, fileName);
                                using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
                                {
                                    outputStream.CopyTo(outputFileStream);
                                }
                            }
                        }
                    }
                    // Repeat for the next month
                    certificationDate = certificationDate.AddMonths(1);
                }
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unable to render a badge"), e, gptwContext);
            }
        }

        [HttpGet("[Action]")]
        public IActionResult CreateBadgeZip(int clientId, int engagementId, string token)
        {
            this.SetTokenFromExternalSource(token);
            GptwLogContext gptwContext = GetNewGptwLogContext("CreateBadge", null);

            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = clientId;
                if (gptwContext.EngagementId <= 0)
                    gptwContext.EngagementId = engagementId;
            }
            catch (Exception) { }

            if (!IsUserAuthorized(gptwContext, clientId))
            {
                AtlasLog.LogError(String.Format("Not Authorized.token:{0}", token), gptwContext);
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                this.ClearTokenFromExternalSource(token);
                return Unauthorized();
            }

            // The token is no longer needed to be overidden at this point so clear it.
            this.ClearTokenFromExternalSource(token);


            // This path produces a zipped archive of badge files in .svg, .png, and .jpg formats. 
            try
            {

                // Create the streamed archive
                ImageBuilder imageBuilder = new ImageBuilder(this.AORepository, gptwContext, AtlasLog);
                Stream archiveStream = imageBuilder.createBadgeZipStream(engagementId);

                // Return the stream for download
                BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(this.AORepository, gptwContext, AtlasLog);
                string badgeFilename = badgeContentHelpers.buildBadgeFilename(engagementId, gptwContext);
                FileStreamResult fs = File(archiveStream, "application/zip", badgeFilename + ".zip");

                AtlasLog.LogInformation(String.Format("Returning a ZIP package of badges"), gptwContext);

                return fs;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unable to render a badge"), e, gptwContext);
                return StatusCode(500);
            }
        }

        [HttpGet("[Action]")]
        public IActionResult CreateShareableImage(int index, int clientId, int engagementId, string imageType, int quality, string token)
        {
            this.SetTokenFromExternalSource(token);
            GptwLogContext gptwContext = GetNewGptwLogContext("CreateShareableImage", null);

            try
            {
                if (gptwContext.ClientId <= 0)
                    gptwContext.ClientId = clientId;
                if (gptwContext.EngagementId <= 0)
                    gptwContext.EngagementId = engagementId;
            }
            catch (Exception) { }

            if (!IsUserAuthorized(gptwContext, clientId))
            {
                AtlasLog.LogError(String.Format("Not Authorized.token:{0}", token), gptwContext);
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                this.ClearTokenFromExternalSource(token);
                return Unauthorized();
            }

            // The token is no longer needed to be overidden at this point so clear it.
            this.ClearTokenFromExternalSource(token);


            // This path produces a shareable image, in an svg format, with dynamically inserted content, and with no text vectorization.
            try
            {
                // Get the template in the resources folder as a stream
                string imageTemplate = String.Format("Portal.Resources.Shareable-Image-{0}-template.svg", index.ToString());
                string outputFilename = "";

                // Gather the dynamic content    
                Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();
                try
                {
                    // Gather the image-specific parameter values
                    switch (index)
                    {
                        case 1:
                            dynamicContent = gatherImage1DynamicContent(engagementId, gptwContext);
                            outputFilename = "We're Certified!";
                            break;
                        case 2:
                            dynamicContent = gatherImage2DynamicContent(engagementId, gptwContext);
                            outputFilename = "Made To Feel Welcome";
                            break;
                        case 3:
                            dynamicContent = gatherImage3DynamicContent(engagementId, gptwContext);
                            break;
                        case 4:
                            dynamicContent = gatherImage4DynamicContent(engagementId, gptwContext);
                            outputFilename = "Amazing Culture!";
                            break;
                        default:
                            throw (new Exception(String.Format("Unsupported shareable image template '{0}'", imageTemplate)));
                    }
                }
                catch (Exception e)
                {
                    AtlasLog.LogErrorWithException(String.Format("Unable to gather content for the shareable image template '{0}'", imageTemplate), e, gptwContext);
                    throw (new Exception(String.Format("Unable to gather content")));
                }

                ImageBuilder imageBuilder = new ImageBuilder(this.AORepository, gptwContext, AtlasLog);
                Stream outputStream = imageBuilder.CreateImageStream(imageType, imageTemplate, dynamicContent);

                // Name to give the delivered file
                string downloadFilename = sanitizeFilename(outputFilename + "." + imageType);

                // Return the result
                string contentType = getContentType(imageType);
                FileStreamResult fs = File(outputStream, contentType, downloadFilename);

                AtlasLog.LogInformation(String.Format("Returning shareable image{0} of type '{1}'", index, imageType), gptwContext);

                return fs;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unable to render a shareable image"), e, gptwContext);
                return StatusCode(500);
            }
        }



        /// <summary>
        /// Gather the content to put in the Image1 template
        /// </summary>
        private Dictionary<string, KeyValuePair<string, string>> gatherImage1DynamicContent(int engagementId, GptwLogContext gptwContext)
        {

            // Bar chart image

            int clientId = 0;
            int surveyVersionId = 0;
            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            string benchmarkScore = "57";
            string copyrightYear = "2021";

            try
            {
                ECRV2 ecrv2 = this.getEcrv2(engagementId);
                if (ecrv2.ECR.SurveyVersionId != null)
                {
                    surveyVersionId = (int)ecrv2.ECR.SurveyVersionId;
                    clientId = ecrv2.ClientId;
                }

                if (clientId == 0 || surveyVersionId == 0)
                {
                    throw new Exception(String.Format("Missing the SurveyVersionId or ClientId from the ECRV2 record for engagementId '{0}'", engagementId));
                }

                // Get the data slice filter from the unified survey
                string filter = getDataFilter(engagementId);

                // Get the statement scores
                Dictionary<string, string> statementScores = getStatementScoresFromBDS(clientId, surveyVersionId, filter, gptwContext);
                string gptwScore = statementScores["57"];

                string companyName = prepareCompanyName(ecrv2.ClientName);

                // Details for the embedded badge
                BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(this.AORepository, gptwContext, AtlasLog);
                BadgeContentHelpers.BadgeInfo badgeInfo = badgeContentHelpers.getBadgeInfo(engagementId, gptwContext);
                string dateRange = badgeInfo.DateRange;
                string badgeCountry = badgeInfo.CountryShortName;
                string badgeYear = badgeInfo.CertYear;
                string summary = formatScore(gptwScore) + " of employees at " + companyName + " say it is a great place to work compared to " + benchmarkScore + "% of employees at a typical U.S.-based company*";
                string copyrightLine = "Source: Great Place to Work® " + copyrightYear + " U.S.National Employee Engagement Study*";

                string companyBarWidth = "";
                double dblGptwScore;
                if (double.TryParse(gptwScore, out dblGptwScore))
                {
                    // The maximum bar width in the template is 646 pixels.
                    companyBarWidth = Math.Round(((dblGptwScore / 100) * 646), 0, MidpointRounding.ToEven).ToString();
                }

                string benchmarkBarWidth = "";
                double dblbenchmarkScore;
                if (double.TryParse(benchmarkScore, out dblbenchmarkScore))
                {
                    // The maximum bar width in the template is 646 pixels.
                    benchmarkBarWidth = Math.Round(((dblbenchmarkScore / 100) * 646), 0, MidpointRounding.ToEven).ToString();
                }

                // The benchmark line horizontal position for a 0% score
                double XPosMin = 56.3;
                // The benchmark line horizontal position for a 100% score
                double XPosMax = 702.5;
                string benchmarkLineXPos = ((((XPosMax - XPosMin) / 100) * dblbenchmarkScore) + XPosMin).ToString();

                dynamicContent.Add("companyName", new KeyValuePair<string, string>(companyName, "foreignObject"));
                dynamicContent.Add("companyNameBar", new KeyValuePair<string, string>(companyName, "text"));
                dynamicContent.Add("gptwScore", new KeyValuePair<string, string>(formatScore(gptwScore), "text"));
                dynamicContent.Add("benchmarkScore", new KeyValuePair<string, string>(formatScore(benchmarkScore), "text"));
                dynamicContent.Add("copyrightLine", new KeyValuePair<string, string>(copyrightLine, "text"));
                dynamicContent.Add("dateRange", new KeyValuePair<string, string>(dateRange, "text"));
                dynamicContent.Add("countryCode", new KeyValuePair<string, string>(badgeCountry, "text"));
                dynamicContent.Add("summary", new KeyValuePair<string, string>(summary, "foreignObject"));
                dynamicContent.Add("companyBar", new KeyValuePair<string, string>(companyBarWidth, "bar"));
                dynamicContent.Add("benchmarkBar", new KeyValuePair<string, string>(benchmarkBarWidth, "bar"));
                dynamicContent.Add("benchmarkLine", new KeyValuePair<string, string>(benchmarkLineXPos, "line"));
            }
            catch (Exception e)
            {
                string errorMessage = "Error while gathering Image1 dynamic content";
                AtlasLog.LogErrorWithException(errorMessage, e, gptwContext);
                throw (new Exception(errorMessage));
            }

            return dynamicContent;
        }

        /// <summary>
        /// Gather the content to put in the Image2 template
        /// </summary>
        private Dictionary<string, KeyValuePair<string, string>> gatherImage2DynamicContent(int engagementId, GptwLogContext gptwContext)
        {

            // Welcome score image

            int clientId = 0;
            int surveyVersionId = 0;
            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            try
            {
                ECRV2 ecrv2 = this.getEcrv2(engagementId);
                if (ecrv2.ECR.SurveyVersionId != null)
                {
                    surveyVersionId = (int)ecrv2.ECR.SurveyVersionId;
                    clientId = ecrv2.ClientId;
                }

                if (clientId == 0 || surveyVersionId == 0)
                {
                    throw new Exception(String.Format("Missing the SurveyVersionId or ClientId from the ECRV2 record for engagementId '{0}'", engagementId));
                }

                // Get the data slice filter from the unified survey
                string filter = getDataFilter(engagementId);

                // Get the statement scores
                Dictionary<string, string> statementScores = getStatementScoresFromBDS(clientId, surveyVersionId, filter, gptwContext);
                string welcomeScore = statementScores["55"];

                string companyName = prepareCompanyName(ecrv2.ClientName);

                // Details for the embedded badge
                BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(this.AORepository, gptwContext, AtlasLog);
                BadgeContentHelpers.BadgeInfo badgeInfo = badgeContentHelpers.getBadgeInfo(engagementId, gptwContext);
                string dateRange = badgeInfo.DateRange;
                string badgeCountry = badgeInfo.CountryShortName;
                string badgeYear = badgeInfo.CertYear;
                string footnote = "Source: " + badgeYear + " Great Place to Work Trust Index® Survey";

                dynamicContent.Add("welcomeScore", new KeyValuePair<string, string>(formatScore(welcomeScore), "text"));
                dynamicContent.Add("dateRange", new KeyValuePair<string, string>(dateRange, "text"));
                dynamicContent.Add("footnote", new KeyValuePair<string, string>(footnote, "text"));
                dynamicContent.Add("countryCode", new KeyValuePair<string, string>(badgeCountry, "text"));
                dynamicContent.Add("companyName", new KeyValuePair<string, string>(companyName, "foreignObject"));
            }
            catch (Exception e)
            {
                string errorMessage = "Error while gathering Image2 dynamic content";
                AtlasLog.LogErrorWithException(errorMessage, e, gptwContext);
                throw (new Exception(errorMessage));
            }

            return dynamicContent;
        }

        /// <summary>
        /// Gather the content to put in the Image3 template
        /// </summary>
        private Dictionary<string, KeyValuePair<string, string>> gatherImage3DynamicContent(int engagementId, GptwLogContext gptwContext)
        {

            // Five scores image

            int clientId = 0;
            int surveyVersionId = 0;
            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            try
            {
                ECRV2 ecrv2 = this.getEcrv2(engagementId);
                if (ecrv2.ECR.SurveyVersionId != null)
                {
                    surveyVersionId = (int)ecrv2.ECR.SurveyVersionId;
                    clientId = ecrv2.ClientId;
                }

                if (clientId == 0 || surveyVersionId == 0)
                {
                    throw new Exception(String.Format("Missing the SurveyVersionId or ClientId from the ECRV2 record for engagementId '{0}'", engagementId));
                }

                // Get the data slice filter from the unified survey
                string filter = getDataFilter(engagementId);

                // Get the statement scores
                Dictionary<string, string> statementScores = getStatementScoresFromBDS(clientId, surveyVersionId, filter, gptwContext);

                // Get scores by statement core id
                string welcomeScore = statementScores["55"];
                string proudScore = statementScores["38"];
                string specialBenefitsScore = statementScores["47"];
                string managementScore = statementScores["49"];
                string fullMemberScore = statementScores["52"];

                string companyName = prepareCompanyName(ecrv2.ClientName);

                // Details for the embedded badge
                BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(this.AORepository, gptwContext, AtlasLog);
                BadgeContentHelpers.BadgeInfo badgeInfo = badgeContentHelpers.getBadgeInfo(engagementId, gptwContext);
                string dateRange = badgeInfo.DateRange;
                string badgeCountry = badgeInfo.CountryShortName;
                string badgeYear = badgeInfo.CertYear;
                string footnote = "Source: " + badgeYear + " Great Place to Work Trust Index® Survey";

                dynamicContent.Add("welcomeScore", new KeyValuePair<string, string>(formatScore(welcomeScore), "text"));
                dynamicContent.Add("proudScore", new KeyValuePair<string, string>(formatScore(proudScore), "text"));
                dynamicContent.Add("specialBenefitsScore", new KeyValuePair<string, string>(formatScore(specialBenefitsScore), "text"));
                dynamicContent.Add("managementScore", new KeyValuePair<string, string>(formatScore(managementScore), "text"));
                dynamicContent.Add("fullMemberScore", new KeyValuePair<string, string>(formatScore(fullMemberScore), "text"));
                dynamicContent.Add("companyName", new KeyValuePair<string, string>(companyName, "foreignObject"));
                dynamicContent.Add("dateRange", new KeyValuePair<string, string>(dateRange, "text"));
                dynamicContent.Add("footnote", new KeyValuePair<string, string>(footnote, "text"));
                dynamicContent.Add("countryCode", new KeyValuePair<string, string>(badgeCountry, "text"));
            }
            catch (Exception e)
            {
                string errorMessage = "Error while gathering Image3 dynamic content";
                AtlasLog.LogErrorWithException(errorMessage, e, gptwContext);
                throw (new Exception(errorMessage));
            }

            return dynamicContent;
        }

        /// <summary>
        /// Gather the content to put in the Image4 template
        /// </summary>
        private Dictionary<string, KeyValuePair<string, string>> gatherImage4DynamicContent(int engagementId, GptwLogContext gptwContext)
        {
            // Generic image

            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            try
            {
                // Details for the embedded badge
                BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(this.AORepository, gptwContext, AtlasLog);
                BadgeContentHelpers.BadgeInfo badgeInfo = badgeContentHelpers.getBadgeInfo(engagementId, gptwContext);
                string dateRange = badgeInfo.DateRange;
                string badgeCountry = badgeInfo.CountryShortName;
                string badgeYear = badgeInfo.CertYear;
                string footnote = "Source: " + badgeYear + " Great Place to Work Trust Index® Survey";

                dynamicContent.Add("dateRange", new KeyValuePair<string, string>(dateRange, "text"));
                dynamicContent.Add("footnote", new KeyValuePair<string, string>(footnote, "text"));
                dynamicContent.Add("countryCode", new KeyValuePair<string, string>(badgeCountry, "text"));
            }
            catch (Exception e)
            {
                string errorMessage = "Error while gathering Image4 dynamic content";
                AtlasLog.LogErrorWithException(errorMessage, e, gptwContext);
                throw (new Exception(errorMessage));
            }

            return dynamicContent;
        }

        private string sanitizeFilename(string filename)
        {
            // Remove any invalid characters from a string intended to be used as a filename
            string sanitizedFilename = string.Join("", filename.Split(Path.GetInvalidFileNameChars()));
            // Also replace spaces with '_'
            sanitizedFilename = sanitizedFilename.Replace(" ", "_");
            return sanitizedFilename;
        }

        private string prepareCompanyName(string companyName)
        {

            if (companyName.StartsWith("Activated Insights - "))
            {
                companyName = companyName.Replace("Activated Insights - ", "");
            }
            return companyName;
        }

        private ECRV2 getEcrv2(int engagementId)
        {
            ECRV2 ecrv2 = this.AORepository.RetrieveReadOnlyECRV2(engagementId);

            return ecrv2;
        }

        private string getContentType(string imageType)
        {

            string contentType = "";

            switch (imageType.ToLower())
            {
                case "svg":
                    {
                        contentType = "image/svg+xml";
                        break;
                    }
                case "jpg":
                case "jpeg":
                    {
                        contentType = "image/jpeg";
                        break;
                    }
                case "png":
                    {
                        contentType = "image/png";
                        break;
                    }
                case "zip":
                    {
                        contentType = "application/zip";
                        break;
                    }
                default:
                    {
                        AtlasLog.LogWarning("No content type defined in getContentType() for the image type '" + imageType + "'");
                        break;
                    }
            }

            return contentType;
        }

        private Dictionary<string, string> getStatementScoresFromBDS(int clientId, int surveyVersionId, string filter, GptwLogContext parentGptwContext)
        {

            Dictionary<string, string> statementScores = new Dictionary<string, string>();

            // For testing only:
            // GptwLogContext gptwContext = GetNewGptwLogContext("GetStatementScoresFromBDS", parentGptwContext);
            // clientId = 5023980;

            try
            {
                HttpResponseMessage result;

                using (var hc = clientFactory.CreateClient())
                {
                    hc.Timeout = new TimeSpan(0, 0, appSettings.HttpClientTimeoutSeconds);
                    string url = appSettings.BusinessDataServicesURL + "/api/trustindex/getdatacolumn";
                    // For testing only
                    // string url = "https://bds.greatplacetowork.com" + "/api/trustindex/getdatacolumn";

                    GetDataColRequest pst = new GetDataColRequest
                    {
                        token = appSettings.BusinessDataServicesToken,
                        cid = clientId,
                        svid = surveyVersionId,
                        filter = filter

                        // For testing only
                        //token = "Ygt%uq!6nZc!dTB7^eLYlLdHsOBt^vE9",
                        //cid = 7011305,
                        //svid = 202096,
                        //filter = filter
                    };

                    String content = JsonConvert.SerializeObject(pst);

                    if (content == null)
                    {
                        //AtlasLog.LogError(String.Format("ERROR:GetStatementScoresFromBDS content = null."), gptwContext);
                        return new Dictionary<string, string>();
                    }

                    StringContent StuffToPost = new StringContent(content);
                    StuffToPost.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    result = hc.PostAsync(url, new StringContent(content, UnicodeEncoding.UTF8, "application/json")).Result;
                }

                if (result.IsSuccessStatusCode)
                {
                    string apiResponse = result.Content.ReadAsStringAsync().Result;
                    var statements = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(apiResponse);

                    // Build a dictionary of statement scores
                    for (int i = 0; i < statements.Count; i++)
                    {
                        string str;
                        string stmt_core_id = "";
                        string statementScore = "N/A";

                        if (statements[i].TryGetValue("stmt_core_id", out str))
                        {
                            stmt_core_id = str;
                            if (statements[i].TryGetValue("PercentPos", out str))
                                statementScore = str;
                            statementScores.Add(stmt_core_id, statementScore);
                        }
                    }

                }
                else
                {
                    //AtlasLog.LogError(String.Format("ERROR:RetrieveFromBDS failed with an http status code of {0}.", result.StatusCode), gptwContext);
                    statementScores = new Dictionary<string, string>();
                }
            }
            catch (Exception e)
            {
                //AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                statementScores = new Dictionary<string, string>();
            }

            return statementScores;
        }

        private string formatScore(string score)
        {

            string formattedScore = "";

            // Reformat the score as a string with an integer value
            // If not an integer (e.g. 'N/A', leave as is
            double result;
            if (double.TryParse(score, out result))
            {
                formattedScore = (Math.Round(result, 0, MidpointRounding.ToEven)).ToString() + "%"; // Banker's rounding
            }

            return formattedScore;
        }

        private IMongoDatabase GetMongoDatabase(string connectionString)
        {

            MongoClient mongoClient = new MongoClient(connectionString);
            IMongoDatabase mongoDaLoDb = mongoClient.GetDatabase("dalosurveys");

            return mongoDaLoDb;
        }

        private BsonDocument getUnifiedSurvey(string connectionString, int engagementId)
        {

            BsonDocument us = null;

            try
            {

                IMongoDatabase unifiedSurveyDb = GetMongoDatabase(connectionString);
                IMongoCollection<BsonDocument> mongoDaLoUnifiedSurveysCollection = unifiedSurveyDb.GetCollection<BsonDocument>("unifiedsurveys");
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("EngagementControlRecordId", engagementId);
                us = mongoDaLoUnifiedSurveysCollection.Find(filter).FirstOrDefault();
            }
            catch (Exception ex)
            {
                AtlasLog.LogErrorWithException("Portal.BadgeController getUnifiedSurvey() failed for engagement " + engagementId.ToString(), ex);
            }

            return us;
        }

        private string getDataFilter(int engagementId)
        {

            string mongoConnectionString = appSettings.MongoDBConnectionString;
            // For testing only
            // string mongoConnectionString = "mongodb://prod_db_user:ga8kebZX94ArdZkJuAghbJvQQ78qhkcDTAyzkeQFre8m4x5tnvAgFr7tVyvAaa8e@prod-mongodb02-shard-00-00-eyvug.azure.mongodb.net:27017,prod-mongodb02-shard-00-01-eyvug.azure.mongodb.net:27017,prod-mongodb02-shard-00-02-eyvug.azure.mongodb.net:27017/?ssl=true&replicaSet=prod-mongodb02-shard-0&authSource=admin&retryWrites=true"; // prod

            // Get the data slice filter from the unified survey

            // For testing only
            // int engagementId = 300059;

            BsonDocument us = getUnifiedSurvey(mongoConnectionString, engagementId);
            string dataFilter = us.GetElement("DataSliceFilter").Value.ToString();
            // for testing
            //string dataFilter = "(respondent_key LIKE '%')";

            return dataFilter;
        }
    }
}
