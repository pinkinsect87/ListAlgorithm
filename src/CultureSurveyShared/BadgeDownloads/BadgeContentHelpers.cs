using System;
using System.IO;
using SharedProject2;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;
using Serilog;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CultureSurveyShared.BadgeDownloads
{
    public class BadgeContentHelpers
    {
        private AtlasOperationsDataRepository badgeContentHelperAORepository;

        public BadgeContentHelpers(AtlasOperationsDataRepository repo)
        {
            badgeContentHelperAORepository = repo;
        }

        public Dictionary<string, KeyValuePair<string, string>> gatherBadgeDynamicContent(int engagementId, string countryCode = "")
        {

            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            BadgeInfo badgeInfo = getBadgeInfo(engagementId, countryCode);

            dynamicContent.Add("countryName", new KeyValuePair<string, string>(badgeInfo.CountryShortName, "text"));
            dynamicContent.Add("dateRange", new KeyValuePair<string, string>(badgeInfo.DateRange, "text"));

            return dynamicContent;
        }

        public BadgeInfo getBadgeInfo(int engagementId, string countryCode = "")
        {

            BadgeInfo badgeInfo = new BadgeInfo();

            try
            {
                ECRV2 ecrv2 = badgeContentHelperAORepository.RetrieveReadOnlyECRV2(engagementId);
                CountryData certifiedCountryData = null;
                if (countryCode == "")
                {
                    certifiedCountryData = (from c in badgeContentHelperAORepository.FindCertificationCountries(ecrv2)
                                            where (String.Equals(c.CertificationStatus, "certified", StringComparison.OrdinalIgnoreCase))
                                            select c).FirstOrDefault();
                }
                else
                {
                    certifiedCountryData = (from c in badgeContentHelperAORepository.FindCertificationCountries(ecrv2)
                                            where (String.Equals(c.CertificationStatus, "certified", StringComparison.OrdinalIgnoreCase) && String.Equals(c.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase))
                                            select c).FirstOrDefault();
                }

                if (certifiedCountryData == null)
                {
                    throw new Exception("No certified countries are defined for engagement " + engagementId.ToString());
                }

                Country country = badgeContentHelperAORepository.GetCountryByCountryCode(certifiedCountryData.CountryCode);
                BadgePreference defaultBadgePreference = getDefaultBadgePreference(country);

                if (defaultBadgePreference.ShortNameForBadge == "")
                {
                    throw new Exception("No ShortNameForBadge is defined for the " + defaultBadgePreference.BadgeVersion + " badge version of country code " + certifiedCountryData.CountryCode);
                }

                DateTime? certStartDate = certifiedCountryData.CertificationDate;
                DateTime? certEndDate = certifiedCountryData.CertificationExpiryDate;

                badgeInfo.CountryShortName = defaultBadgePreference.ShortNameForBadge;
                badgeInfo.LanguageCode = defaultBadgePreference.CultureId;
                badgeInfo.DateRange = buildBadgeDateRange(certStartDate, certEndDate, defaultBadgePreference.CultureId);
                badgeInfo.CertYear = certifiedCountryData.CertificationDate.HasValue ? certifiedCountryData.CertificationDate.Value.Year.ToString() : "";

                //string assemblyName = Assembly.GetCallingAssembly().GetName().Name;
                string assemblyName = getAssemblyName();
                badgeInfo.BadgeTemplate = String.Format("{0}.Resources.{1}", assemblyName, defaultBadgePreference.BadgeTemplate);
            }
            catch (Exception e)
            {
                string errorMessage = "Error while getting badge info";
                Serilog.Log.Error(e, "BadgeContentHelpers.getBadgeInfo: errorMessage");
                throw (new Exception(errorMessage));
            }

            return badgeInfo;
        }

        public String getAssemblyName()
        {
            string assemblyName = Assembly.GetCallingAssembly().GetName().Name;
            return assemblyName;
        }

        public BadgePreference getDefaultBadgePreference(Country country)
        {
            BadgePreference defaultBadgePreference = new BadgePreference();
            bool haveDefaultPreference = false;

            try
            {
                foreach (BadgePreference badgePref in country.BadgePreferences)
                {
                    if (badgePref.BadgeDefault.ToLower() == "yes")
                    {
                        defaultBadgePreference = badgePref;
                        haveDefaultPreference = true;
                        break;
                    }
                }
                if (!haveDefaultPreference)
                {
                    throw new Exception("No default badge preference defined for country code " + country.CountryCode);
                }
            }
            catch (Exception e)
            {
                string errorMessage = "Error while getting badge info";
                Serilog.Log.Error(e, "getDefaultBadgePreference.BadgeContentHelpers.getBadgeInfo: errorMessage");
                throw (new Exception(errorMessage));
            }

            return defaultBadgePreference;
        }

        public string buildBadgeDateRange(DateTime? startCertificationDate, DateTime? endCertificationDate, string badgeLanguageCode)
        {
            string badgeDateRange = "";

            if (startCertificationDate.HasValue && endCertificationDate.HasValue)
            {
                if (badgeLanguageCode == "fi-FI")
                {
                    // Finnish exception - reads as full month followed by the year range
                    string badgeMonth = startCertificationDate.Value.ToString("MMM", CultureInfo.CreateSpecificCulture(badgeLanguageCode)).ToUpper();
                    string badgeYears = startCertificationDate.Value.ToString("yyyy") + "-" + endCertificationDate.Value.ToString("yyyy");
                    badgeDateRange = badgeMonth + " " + badgeYears;
                }
                else
                {
                    // Everyone else
                    string startBadgeDate = startCertificationDate.Value.ToString("MMM yyyy", CultureInfo.CreateSpecificCulture(badgeLanguageCode)).ToUpper();
                    string endBadgeDate = endCertificationDate.Value.ToString("MMM yyyy", CultureInfo.CreateSpecificCulture(badgeLanguageCode)).ToUpper();
                    badgeDateRange = startBadgeDate + "-" + endBadgeDate;
                }
            }
            // Remove spurious 'dots' from month abbreviations
            badgeDateRange = badgeDateRange.Replace(".", "");

            return badgeDateRange;
        }

        public string buildBadgeFilename(int engagementId, string countryCode = "")
        {
            string badgeFilename = "";

            try
            {
                string companyName = getCompanyName(engagementId);
                BadgeInfo badgeInfo = getBadgeInfo(engagementId, countryCode);
                ImageBuilder imageBuilder = new ImageBuilder(badgeContentHelperAORepository);
                if (countryCode == "")
                {
                    badgeFilename = sanitizeFilename(String.Format("{0}_{1}_Certification_Badge", companyName, badgeInfo.CertYear));
                }
                else
                {
                    badgeFilename = sanitizeFilename(String.Format("{0}_{1}_{2}_Certification_Badge", companyName, countryCode, badgeInfo.CertYear));
                }
                return badgeFilename;
            }
            catch (Exception e)
            {
                string errorMessage = "Error while building a badge filename";
                Serilog.Log.Error(e, "buildBadgeFilename: " + errorMessage);
                throw (new Exception(errorMessage));
            }
        }

        private string getCompanyName(int engagementId)
        {
            ECRV2 ecrv2 = badgeContentHelperAORepository.RetrieveReadOnlyECRV2(engagementId);
            string companyName = prepareCompanyName(ecrv2.ClientName);

            return companyName;
        }

         private string sanitizeFilename(string filename)
        {
            // Remove any invalid characters from a string intended to be used as a filename
            string sanitizedFilename = string.Join("", filename.Split(Path.GetInvalidFileNameChars()));
            // Also replace spaces with '_'
            sanitizedFilename = sanitizedFilename.Replace(" ", "_");
            return sanitizedFilename;
        }

        public string makeDownloadableFilename(int engagementId, string imageType)
        {

            string filename = buildBadgeFilename(engagementId);
            string downloadFilename = sanitizeFilename(filename + "." + imageType);
            return downloadFilename;
        }

        private string prepareCompanyName(string companyName)
        {

            if (companyName.StartsWith("Activated Insights - "))
            {
                companyName = companyName.Replace("Activated Insights - ", "");
            }
            return companyName;
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
                Serilog.Log.Error(ex, String.Format("BadgeContentHelpers.getUnifiedSurvey failed for engagement '{0}'", engagementId));
            }

            return us;
        }
    }

    public class BadgeInfo
    {
        public string CountryShortName = "";
        public string LanguageCode = "";
        public string DateRange = "";
        public string CertYear = "";
        public string BadgeTemplate = "";
    }

    public class DataColRequestParams
    {
        public string token;
        public int cid;
        public int svid;
        public string filter;
    }
}

