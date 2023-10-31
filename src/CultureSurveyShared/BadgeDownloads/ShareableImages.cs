using SharedProject2;
using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using System.Linq;

namespace CultureSurveyShared.BadgeDownloads
{
    public class ShareableImages
    {

        private string _daloMongoConnectionString;
        private string _mongoLockAzureStorageConnectionString;
        private string _businessDataServicesURL;
        private string _businessDataServicesToken;
        private int _httpClientTimeoutSeconds;

        public ShareableImages(string daloMongoConnectionString, string mongoLockAzureStorageConnectionString, string businessDataServicesURL, string businessDataServicesToken, int httpClientTimeoutSeconds)
        {
            _daloMongoConnectionString = daloMongoConnectionString;
            _mongoLockAzureStorageConnectionString = mongoLockAzureStorageConnectionString;
            _businessDataServicesURL = businessDataServicesURL;
            _businessDataServicesToken = businessDataServicesToken;
            _httpClientTimeoutSeconds = httpClientTimeoutSeconds;
        }

        public Stream CreateShareableImage(int index, int engagementId, string countryCode, string imageType, int quality)
        {

            // This produces a single shareable image, with dynamically inserted content, and with no text vectorization. 
            try
            {
                AtlasOperationsDataRepository atlasOperationsRepo = new AtlasOperationsDataRepository(_daloMongoConnectionString, _mongoLockAzureStorageConnectionString);
                ECRV2 ecrv2 = atlasOperationsRepo.RetrieveReadOnlyECRV2(engagementId);

                BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(atlasOperationsRepo);

                // Get the shareable image template to be modified
                BadgeInfo badgeInfo = badgeContentHelpers.getBadgeInfo(engagementId, countryCode);

                string filter = getDataFilter(atlasOperationsRepo, engagementId);

                string shareableImageTemplate = String.Format("{0}.Resources.Shareable-Image-{1}-template.svg", badgeContentHelpers.getAssemblyName(), index);

                // Gather the dynamic content    
                Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();
                try
                {
                    // Gather the image-specific parameter values
                    switch (index)
                    {
                        case 1:
                            dynamicContent = gatherImage1DynamicContent(engagementId, atlasOperationsRepo, badgeInfo);
                            break;
                        case 2:
                            dynamicContent = gatherImage2DynamicContent(engagementId, atlasOperationsRepo, badgeInfo);
                            break;
                        case 3:
                            dynamicContent = gatherImage3DynamicContent(engagementId, atlasOperationsRepo, badgeInfo);
                            break;
                        case 4:
                            dynamicContent = gatherImage4DynamicContent(badgeInfo);
                            break;
                        default:
                            throw (new Exception(String.Format("Unsupported shareable image number '{0}'", index)));
                    }
                }
                catch (Exception e)
                {
                    Serilog.Log.Error(e, String.Format("CreateShareableImage: Unable to gather content for the shareable image template '{0}'", shareableImageTemplate));
                    throw (new Exception(String.Format("Unable to gather content")));
                }

                // Create the shareable image as a stream
                ImageBuilder imageBuilder = new ImageBuilder(atlasOperationsRepo);
                Stream outputStream = imageBuilder.CreateImageStream(imageType, shareableImageTemplate, dynamicContent);

                Serilog.Log.Information(String.Format("CreateShareableImage: Returning a shareable image of type '{0}' for engagement '{0}'", index, engagementId));

                return outputStream;
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "Unable to render a shareable image");
                return null;
            }
        }

        public string getShareableImageFilename(int index)
        {
            string outputFilename = "";
            
            switch (index)
            {
                case 1:
                    outputFilename = "We're Certified!";
                    break;
                case 2:
                    outputFilename = "Made To Feel Welcome";
                    break;
                case 3:
                    // this shareable image is no longer distributed
                    break;
                case 4:
                    outputFilename = "Amazing Culture!";
                    break;
                default:
                    throw (new Exception(String.Format("Unsupported shareable image number '{0}'", index)));
            }

            // Remove any invalid characters from a string intended to be used as a filename
            string sanitizedFilename = string.Join("", outputFilename.Split(Path.GetInvalidFileNameChars()));
            // Also replace spaces with '_'
            sanitizedFilename = sanitizedFilename.Replace(" ", "_");

            return sanitizedFilename;
        }

        /// <summary>
        /// Gather the content to put in the Image1 template
        /// </summary>
        public Dictionary<string, KeyValuePair<string, string>> gatherImage1DynamicContent(int engagementId, AtlasOperationsDataRepository atlasOperationsRepo, BadgeInfo badgeInfo)
        {

            ECRV2 ecrv2 = atlasOperationsRepo.RetrieveReadOnlyECRV2(engagementId);

            // Bar chart image

            int clientId = 0;
            int surveyVersionId = 0;
            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            string benchmarkScore = "57";
            string copyrightYear = "2021";

            try
            {
                if (ecrv2.ECR.SurveyVersionId != null)
                {
                    surveyVersionId = (int)ecrv2.ECR.SurveyVersionId;
                    clientId = ecrv2.ClientId;
                }

                if (clientId == 0 || surveyVersionId == 0)
                {
                    throw new Exception(String.Format("Missing the SurveyVersionId or ClientId from the ECRV2 record for engagementId '{0}'", engagementId));
                }

                // US-835 Get the data slice filter by finding the first certified country within the ECR
                string filter = getDataFilter(atlasOperationsRepo, engagementId);

                // Get the statement scores
                Dictionary<string, string> statementScores = BDSHelpers.getStatementScoresFromBDS(clientId, surveyVersionId, filter, _businessDataServicesURL, _businessDataServicesToken, _httpClientTimeoutSeconds);
                string gptwScore = statementScores["57"];

                string companyName = prepareCompanyName(ecrv2.ClientName);

                // Details for the embedded badge
                string dateRange = badgeInfo.DateRange;
                string badgeCountry = badgeInfo.CountryShortName;
                string badgeYear = badgeInfo.CertYear;
                string summary = formatScore(gptwScore) + " of employees at " + companyName + " say it is a great place to work compared to " + benchmarkScore + "% of employees at a typical U.S.-based company*";
                string copyrightLine = "Source: Great Place To Work® " + copyrightYear + " U.S.National Employee Engagement Study*";

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
                Serilog.Log.Error(e, String.Format("BadgeContentHelpers.gatherImage1DynamicContent: {0}", errorMessage));
                throw (new Exception(errorMessage));
            }

            return dynamicContent;
        }

        /// <summary>
        /// Gather the content to put in the Image2 template
        /// </summary>
        public Dictionary<string, KeyValuePair<string, string>> gatherImage2DynamicContent(int engagementId, AtlasOperationsDataRepository atlasOperationsRepo, BadgeInfo badgeInfo)
        {

            ECRV2 ecrv2 = atlasOperationsRepo.RetrieveReadOnlyECRV2(engagementId);

            // Welcome score image

            int clientId = 0;
            int surveyVersionId = 0;
            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            try
            {
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
                string filter = getDataFilter(atlasOperationsRepo, engagementId);

                // Get the statement scores
                Dictionary<string, string> statementScores = BDSHelpers.getStatementScoresFromBDS(clientId, surveyVersionId, filter, _businessDataServicesURL, _businessDataServicesToken, _httpClientTimeoutSeconds);
                string welcomeScore = statementScores["55"];

                string companyName = prepareCompanyName(ecrv2.ClientName);

                // Details for the embedded badge
                string dateRange = badgeInfo.DateRange;
                string badgeCountry = badgeInfo.CountryShortName;
                string badgeYear = badgeInfo.CertYear;
                string footnote = "Source: " + badgeYear + " Great Place To Work Trust Index® Survey";

                dynamicContent.Add("welcomeScore", new KeyValuePair<string, string>(formatScore(welcomeScore), "text"));
                dynamicContent.Add("dateRange", new KeyValuePair<string, string>(dateRange, "text"));
                dynamicContent.Add("footnote", new KeyValuePair<string, string>(footnote, "text"));
                dynamicContent.Add("countryCode", new KeyValuePair<string, string>(badgeCountry, "text"));
                dynamicContent.Add("companyName", new KeyValuePair<string, string>(companyName, "foreignObject"));
            }
            catch (Exception e)
            {
                string errorMessage = "Error while gathering Image2 dynamic content";
                Serilog.Log.Error(e, String.Format("BadgeContentHelpers.gatherImage2DynamicContent: {0}", errorMessage));
                throw (new Exception(errorMessage));
            }

            return dynamicContent;
        }

        /// <summary>
        /// Gather the content to put in the Image3 template
        /// </summary>
        public Dictionary<string, KeyValuePair<string, string>> gatherImage3DynamicContent(int engagementId, AtlasOperationsDataRepository atlasOperationsRepo, BadgeInfo badgeInfo)
        {

            ECRV2 ecrv2 = atlasOperationsRepo.RetrieveReadOnlyECRV2(engagementId);

            // Five scores image

            int clientId = 0;
            int surveyVersionId = 0;
            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            try
            {
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
                string filter = getDataFilter(atlasOperationsRepo, engagementId);

                // Get the statement scores
                Dictionary<string, string> statementScores = BDSHelpers.getStatementScoresFromBDS(clientId, surveyVersionId, filter, _businessDataServicesURL, _businessDataServicesToken, _httpClientTimeoutSeconds);

                // Get scores by statement core id
                string welcomeScore = statementScores["55"];
                string proudScore = statementScores["38"];
                string specialBenefitsScore = statementScores["47"];
                string managementScore = statementScores["49"];
                string fullMemberScore = statementScores["52"];

                string companyName = prepareCompanyName(ecrv2.ClientName);

                // Details for the embedded badge
                string dateRange = badgeInfo.DateRange;
                string badgeCountry = badgeInfo.CountryShortName;
                string badgeYear = badgeInfo.CertYear;
                string footnote = "Source: " + badgeYear + " Great Place To Work Trust Index® Survey";

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
                Serilog.Log.Error(e, String.Format("BadgeContentHelpers.gatherImage3DynamicContent: {0}", errorMessage));
                throw (new Exception(errorMessage));
            }

            return dynamicContent;
        }

        /// <summary>
        /// Gather the content to put in the Image4 template
        /// </summary>
        public Dictionary<string, KeyValuePair<string, string>> gatherImage4DynamicContent(BadgeInfo badgeInfo)
        {
            // Generic image

            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            try
            {
                // Details for the embedded badge
                string dateRange = badgeInfo.DateRange;
                string badgeCountry = badgeInfo.CountryShortName;
                string badgeYear = badgeInfo.CertYear;
                string footnote = "Source: " + badgeYear + " Great Place To Work Trust Index® Survey";

                dynamicContent.Add("dateRange", new KeyValuePair<string, string>(dateRange, "text"));
                dynamicContent.Add("footnote", new KeyValuePair<string, string>(footnote, "text"));
                dynamicContent.Add("countryCode", new KeyValuePair<string, string>(badgeCountry, "text"));
            }
            catch (Exception e)
            {
                string errorMessage = "Error while gathering Image4 dynamic content";
                Serilog.Log.Error(e, String.Format("BadgeContentHelpers.gatherImage4DynamicContent: {0}", errorMessage));
                throw (new Exception(errorMessage));
            }

            return dynamicContent;
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

        private string getDataFilter(AtlasOperationsDataRepository atlasOperationsRepo, int engagementId)
        {
            // US-835
            string firstCertifiedCountryDataSliceFilter = "";
            try
            {
                ECRV2 ecrv2 = atlasOperationsRepo.RetrieveReadOnlyECRV2(engagementId);
                firstCertifiedCountryDataSliceFilter = (from c in atlasOperationsRepo.FindCertificationCountries(ecrv2)
                                                        where (String.Equals(c.CertificationStatus, "certified", StringComparison.OrdinalIgnoreCase))
                                                        select c.DataSliceFilter).FirstOrDefault();

                if (firstCertifiedCountryDataSliceFilter == null)
                {
                    Serilog.Log.Error("CreateShareableImage.getDataFilter: returned a null for engagement " + engagementId.ToString());
                    firstCertifiedCountryDataSliceFilter = "";
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "CreateShareableImage.getDataFilter: failed for engagement " + engagementId.ToString());
            }

            return firstCertifiedCountryDataSliceFilter;
        }

        private string prepareCompanyName(string companyName)
        {

            if (companyName.StartsWith("Activated Insights - "))
            {
                companyName = companyName.Replace("Activated Insights - ", "");
            }
            return companyName;
        }
    }
}
