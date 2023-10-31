using SharedProject2;
using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using System.IO.Compression;
using System.Reflection;

namespace CultureSurveyShared.BadgeDownloads
{
    public class Badges
    {

        private string _daloMongoConnectionString;
        private string _mongoLockAzureStorageConnectionString;
        private string _businessDataServicesURL;
        private string _businessDataServicesToken;
        private int _httpClientTimeoutSeconds;

        public Badges(string daloMongoConnectionString, string mongoLockAzureStorageConnectionString, string businessDataServicesURL = null, string businessDataServicesToken = null, int httpClientTimeoutSeconds = 0)
        {
            _daloMongoConnectionString = daloMongoConnectionString;
            _mongoLockAzureStorageConnectionString = mongoLockAzureStorageConnectionString;
            _businessDataServicesURL = businessDataServicesURL;
            _businessDataServicesToken = businessDataServicesToken;
            _httpClientTimeoutSeconds = httpClientTimeoutSeconds;
        }

        /// <summary>
        /// Create a badge in a specified format for one country
        /// </summary>
        /// <param name="engagementId"></param>
        /// <param name="imageType">"svg", "jpg", or "png"</param>
        /// <param name="countryCode">Country to produce the badge for</param>
        /// <returns>Stream object of a badge file</returns>
        /// <remarks>If the country code is empty, the country chosen will be random if the engagement contains more than one certified country</remarks>
        public Stream CreateBadge(int engagementId, string imageType, string countryCode = "")
        {

            // This produces a single badge, in an svg format, with dynamically inserted content, and with no text vectorization. 
            try
            {
                if (_daloMongoConnectionString == null || _mongoLockAzureStorageConnectionString == null)
                {
                    throw new Exception("Missing data repository connection parameters");
                }

                AtlasOperationsDataRepository atlasOperationsRepo = new AtlasOperationsDataRepository(_daloMongoConnectionString, _mongoLockAzureStorageConnectionString);

                BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(atlasOperationsRepo);

                // Get the badge template to be modified
                BadgeInfo badgeInfo = badgeContentHelpers.getBadgeInfo(engagementId, countryCode);
                string badgeTemplate = badgeInfo.BadgeTemplate;

                // Gather the dynamic content    
                Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();
                try
                {
                    dynamicContent = badgeContentHelpers.gatherBadgeDynamicContent(engagementId, countryCode);
                }
                catch (Exception e)
                {
                    Serilog.Log.Error(e, String.Format("CreateBadge: Unable to gather content for the shareable badge template '{0}'", badgeTemplate));
                    throw (new Exception(String.Format("Unable to gather content")));
                }

                // Create the badge as a stream
                ImageBuilder imageBuilder = new ImageBuilder(atlasOperationsRepo);
                Stream outputStream = imageBuilder.CreateImageStream(imageType, badgeTemplate, dynamicContent);

                Serilog.Log.Information(String.Format("CreateBadge: Returning a badge of type '{0}' for engagement '{1}'", imageType, engagementId));

                return outputStream;
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "CreateBadge: Unable to render a badge");
                return null;
            }
        }

        /// <summary>
        /// Create a zipped package of badges in SVG, JPG, and PNG formats for one country in a given engagement
        /// </summary>
        /// <param name="engagementId">engagement id</param>
        /// <returns>Stream object of a zipped package</returns>
        /// <remarks>The certified country chosen will be random if the country is certified in more than one country</remarks>
        public Stream CreateBadgeZip(int engagementId)
        {

            Stream archiveStream;

            try
            {
                if (_daloMongoConnectionString == null || _mongoLockAzureStorageConnectionString == null)
                {
                    throw new Exception("Missing data repository connection parameters");
                }

                AtlasOperationsDataRepository atlasOperationsRepo = new AtlasOperationsDataRepository(_daloMongoConnectionString, _mongoLockAzureStorageConnectionString);

                ImageBuilder imageBuilder = new ImageBuilder(atlasOperationsRepo);
                archiveStream = imageBuilder.createBadgeZipStream(engagementId);
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "CreateBadgeZip: Unable to create a zip package of badges");
                return null;
            }
            Serilog.Log.Information(String.Format("CreateBadgeZip: Returning an archive stream of badges for engagement '{0}'", engagementId));

            return archiveStream;
        }

        ///// <summary>
        ///// FOR TESTING ONLY
        ///// the 'BADGE ZIP FILE' button on the Certification Toolkit page is being hijacked here to test multi-country badge generation instead
        ///// </summary>
        //public Stream CreateBadgeZip(int engagementId)
        //{

        //    // ***** FOR TESTING THE CREATION OF A MULTI-COUNTRY PACKAGE OF BADGES *****

        //    // 1. Uncomment this version of CreateBadgeZip, and comment out the actual CreateBadgeZip version above
        //    // 2. enter one or more key-value pairs to the countryEngagements dictionary, below
        //    // 3. when done testing, comment out this version of CreateBadgeZip and uncomment the above true version

        //    Stream archiveStream;

        //    Dictionary<string, int> countryEngagements = new Dictionary<string, int>();
        //    // You may add country-engagement pairs of your choice.  
        //    countryEngagements.Add("CA", 52908); // These two key-value pairs will test the creation of a package with a two country engagement (CA, AR)
        //    countryEngagements.Add("AR", 52908);
        //    // countryEngagements.Add("US", 52926); // This key-value pair will test that a US country will include the profile link and embad code text files
        //    archiveStream = CreatecountryEngagementsZip(countryEngagements);

        //    return archiveStream;
        //}

        /// <summary>
        /// Create a zipped package of badges, shareable images, and other client documents
        /// </summary>
        /// <param name="countryEngagements">Dictionary of country-engagement key-value pairs to create badges for</param>
        /// <returns>Stream object of a zipped package</returns>
        public Stream CreatecountryEngagementsZip(Dictionary<string, int> countryEngagements)
        {

            if (_daloMongoConnectionString == null || _mongoLockAzureStorageConnectionString == null || _businessDataServicesURL == null || _businessDataServicesToken == null || _httpClientTimeoutSeconds == 0)
            {
                throw new Exception("Missing or invalid connection parameters");
            }

            AtlasOperationsDataRepository atlasOperationsRepo = new AtlasOperationsDataRepository(_daloMongoConnectionString, _mongoLockAzureStorageConnectionString);
            BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(atlasOperationsRepo);
            ImageBuilder imageBuilder = new ImageBuilder(atlasOperationsRepo);

            // Create Zip archive
            Stream archiveStream = new MemoryStream();
            using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
            {

                // Add the ststic badge guidelines document
                string guidelinesFilename = "gptw_certification_badge_guidelines.pdf";
                using (Stream guidelinesStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CultureSurveyShared.Resources." + guidelinesFilename))
                {
                    ZipHelpers.AddStreamToZipArchive(archive, guidelinesStream, guidelinesFilename);
                }

                // Add the static press release template
                string pressReleaseFilename = "2023-certification-toolkit-sample-press-release.docx";
                using (Stream pressReleaseStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CultureSurveyShared.Resources." + pressReleaseFilename))
                {
                    ZipHelpers.AddStreamToZipArchive(archive, pressReleaseStream, pressReleaseFilename);
                }

                // Add the dynamic badges and shareable images
                foreach (KeyValuePair<string, int> countryEngagement in countryEngagements)
                {
                    string countryCode = countryEngagement.Key;
                    int engagementId = countryEngagement.Value;

                    BadgeInfo badgeInfo = badgeContentHelpers.getBadgeInfo(engagementId, countryCode);
                    string badgeTemplate = badgeInfo.BadgeTemplate;
                    string folderPath = badgeInfo.CountryShortName;

                    // Get the dynamic content (cert date range, short country name) to go on the badge
                    Dictionary<string, KeyValuePair<string, string>> dynamicContent = badgeContentHelpers.gatherBadgeDynamicContent(engagementId, countryCode);

                    string badgeFilename = badgeContentHelpers.buildBadgeFilename(engagementId, countryCode);
                    // Generate the badge in each of the tree supported formats and add to the zip archive
                    imageBuilder.AddBadgesToZipArchive(archive, badgeTemplate, dynamicContent, folderPath + @"\" + badgeFilename);

                    // Add the shareable images
                    ShareableImages shareableImages = new ShareableImages(_daloMongoConnectionString, _mongoLockAzureStorageConnectionString, _businessDataServicesURL, _businessDataServicesToken, _httpClientTimeoutSeconds);

                    string filePath = "";
                    string imageType = "png";

                    // Shareable image 1
                    //Stream shareableImageStream1 = shareableImages.CreateShareableImage(1, engagementId, countryCode, imageType, 0);
                    //filePath = String.Format(@"{0}\{1}_{2}.{3}", folderPath, shareableImages.getShareableImageFilename(1), countryCode, imageType);
                    //ZipHelpers.AddStreamToZipArchive(archive, shareableImageStream1, filePath);

                    // Shareable image 2
                    //Stream shareableImageStream2 = shareableImages.CreateShareableImage(2, engagementId, countryCode, imageType, 0);
                    //filePath = String.Format(@"{0}\{1}_{2}.{3}", folderPath, shareableImages.getShareableImageFilename(2), countryCode, imageType);
                    //ZipHelpers.AddStreamToZipArchive(archive, shareableImageStream2, filePath);

                    // Shareable image 3 is no longer distributed

                    // Shareable image 4
                    //Stream shareableImageStream4 = shareableImages.CreateShareableImage(4, engagementId, countryCode, imageType, 0);
                    //filePath = String.Format(@"{0}\{1}_{2}.{3}", folderPath, shareableImages.getShareableImageFilename(4), countryCode, imageType);
                    //ZipHelpers.AddStreamToZipArchive(archive, shareableImageStream4, filePath);

                    if (countryCode == "US")
                    {
                        // Need the client id, get it from the ecr
                        ECRV2 ecrv2 = atlasOperationsRepo.RetrieveReadOnlyECRV2(engagementId);
                        int clientId = ecrv2.ClientId;

                        // Add the embed code in a text file
                        string embedCode = String.Format(@"<a href=""http://www.greatplacetowork.com/certified-company/{0}"" title=""Rating and Review"" target=""_blank""><img src=""https://www.greatplacetowork.com/images/profiles/{1}/companyBadge.png"" alt=""Review"" width=""120"" ></a>", clientId, clientId);
                        ZipHelpers.AddStringToZipArchive(archive, embedCode, "badge_embed_code.txt");

                        // Add the company profile link in a text file
                        string companyProfileLink = String.Format(@"https://www.greatplacetowork.com/certified-company/{0}", clientId);
                        ZipHelpers.AddStringToZipArchive(archive, companyProfileLink, "company_profile_link.txt");
                    }
                }
            }

            archiveStream.Position = 0;

            return archiveStream;
        }

        /// <summary>
        /// Create a collection of badges for all months between the specified start and end certification dates
        /// THIS IS ONLY ACCESSIBLE VIA UNIT TEST.  IT FULFILLS ONE-OFF REQUESTS TO PRODUCE BADGES IN BULK FOR DISTRIBUTION TO LICENSEES
        /// </summary>
        /// <param name="startCertificationDate">Starting certification date</param>
        /// <param name="endCertificationDate">Ending certification date</param>
        /// <param name="countryCode">Optional country code. If provided, badges will be produced only for that country, otherwise badges will be produced for all countries</param>
        public void CreateAllCountryBadges(DateTime startCertificationDate, DateTime endCertificationDate, string countryCode = "")
        {

            AtlasOperationsDataRepository atlasOperationsRepo = new AtlasOperationsDataRepository(_daloMongoConnectionString, _mongoLockAzureStorageConnectionString);

            BadgeContentHelpers badgeContentHelpers = new BadgeContentHelpers(atlasOperationsRepo);

            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            // Get countries
            List<Country> allCountries = new List<Country>();
            if (countryCode == "")
            {
                // Get all countries
                allCountries = atlasOperationsRepo.GetAllCountries();
            }
            else
            {
                // Get the specified country
                Country country = atlasOperationsRepo.GetCountryByCountryCode(countryCode);
                allCountries.Add(country);
            }

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
                            string badgeTemplate = String.Format("{0}.Resources.{1}", badgeContentHelpers.getAssemblyName(), badgePreference.BadgeTemplate);
                            string badgeCountry = badgePreference.ShortNameForBadge;
                            dynamicContent.Add("countryName", new KeyValuePair<string, string>(badgeCountry, "text"));
                            string badgeDateRange = badgeContentHelpers.buildBadgeDateRange(certificationDate, certificationDate.AddMonths(12), badgePreference.CultureId);
                            dynamicContent.Add("dateRange", new KeyValuePair<string, string>(badgeDateRange, "text"));

                            // Create the badge as a stream
                            ImageBuilder imageBuilder = new ImageBuilder(atlasOperationsRepo);
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
        public string getContentType(string imageType)
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
                        Serilog.Log.Information("No content type defined in getContentType() for the image type '" + imageType + "'" + imageType);
                        break;
                    }
            }

            return contentType;
        }
    }

}
