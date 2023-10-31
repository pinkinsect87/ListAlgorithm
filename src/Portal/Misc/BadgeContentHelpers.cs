using System;
using System.IO;
using SharedProject2;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace Portal.Misc
{
    public class BadgeContentHelpers
    {
        private GptwLog AtlasLog;
        private GptwLogContext gptwContext;
        private AtlasOperationsDataRepository badgeContentHelperAORepository;

        public BadgeContentHelpers(AtlasOperationsDataRepository repo, GptwLogContext gptwContext, GptwLog AtlasLog)
        {
            this.badgeContentHelperAORepository = repo;
            this.gptwContext = gptwContext;
            this.AtlasLog = AtlasLog;
        }

        public Dictionary<string, KeyValuePair<string, string>> gatherBadgeDynamicContent(int engagementId, GptwLogContext gptwContext)
        {

            Dictionary<string, KeyValuePair<string, string>> dynamicContent = new Dictionary<string, KeyValuePair<string, string>>();

            BadgeInfo badgeInfo = getBadgeInfo(engagementId, gptwContext);

            dynamicContent.Add("countryName", new KeyValuePair<string, string>(badgeInfo.CountryShortName, "text"));
            dynamicContent.Add("dateRange", new KeyValuePair<string, string>(badgeInfo.DateRange, "text"));

            return dynamicContent;
        }

        public BadgeInfo getBadgeInfo(int engagementId, GptwLogContext gptwContext)
        {

            BadgeInfo badgeInfo = new BadgeInfo();

            try
            {
                ECRV2 ecrv2 = badgeContentHelperAORepository.RetrieveReadOnlyECRV2(engagementId);
                CountryData certifiedCountryData = (from c in badgeContentHelperAORepository.FindCertificationCountries(ecrv2)
                                                    where (String.Equals(c.CertificationStatus, "certified", StringComparison.OrdinalIgnoreCase))
                                                    select c).FirstOrDefault();

                if (certifiedCountryData == null)
                {
                    throw new Exception("No certified countries are defined for engagement " + engagementId.ToString());
                }

                Country country = badgeContentHelperAORepository.GetCountryByCountryCode(certifiedCountryData.CountryCode);
                BadgePreference defaultBadgePreference = getDefaultBadgePreference(country, gptwContext);

                if (defaultBadgePreference.ShortNameForBadge == "")
                {
                    throw new Exception("No ShortNameForBadge is defined for the " + defaultBadgePreference.BadgeVersion + " badge version of country code " + certifiedCountryData.CountryCode);
                }

                DateTime? certStartDate = certifiedCountryData.CertificationDate;
                DateTime? certEndDate = certifiedCountryData.CertificationExpiryDate;

                badgeInfo.CountryShortName = defaultBadgePreference.ShortNameForBadge;
                badgeInfo.LanguageCode = defaultBadgePreference.CultureId;
                badgeInfo.DateRange = buildBadgeDateRange(certStartDate, certEndDate, defaultBadgePreference.CultureId);
                badgeInfo.CertYear = certifiedCountryData.CertificationDate.Value.Year.ToString();
                badgeInfo.BadgeTemplate = "Portal.Resources." + defaultBadgePreference.BadgeTemplate;
            }
            catch (Exception e)
            {
                string errorMessage = "Error while getting badge info";
                AtlasLog.LogErrorWithException(errorMessage, e, gptwContext);
                throw (new Exception(errorMessage));
            }

            return badgeInfo;
        }

        public BadgePreference getDefaultBadgePreference(Country country, GptwLogContext gptwContext)
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
                AtlasLog.LogErrorWithException(errorMessage, e, gptwContext);
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

        public string buildBadgeFilename(int engagementId, GptwLogContext gptwContext)
        {
            string badgeFilename = "";

            try
            {
                string companyName = getCompanyName(engagementId);
                BadgeInfo badgeInfo = getBadgeInfo(engagementId, gptwContext);
                ImageBuilder imageBuilder = new ImageBuilder(badgeContentHelperAORepository, gptwContext, AtlasLog);
                badgeFilename = sanitizeFilename(companyName + "_" + badgeInfo.CertYear + "_Certification_Badge");
                return badgeFilename;
            }
            catch (Exception e)
            {
                string errorMessage = "Error while building a badge filename";
                AtlasLog.LogErrorWithException(errorMessage, e, gptwContext);
                throw (new Exception(errorMessage));
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

        private string getCompanyName(int engagementId)
        {
            ECRV2 ecrv2 = badgeContentHelperAORepository.RetrieveReadOnlyECRV2(engagementId);
            string companyName = prepareCompanyName(ecrv2.ClientName);

            return companyName;
        }

        private string prepareCompanyName(string companyName)
        {

            if (companyName.StartsWith("Activated Insights - "))
            {
                companyName = companyName.Replace("Activated Insights - ", "");
            }
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

            string filename = getCompanyName(engagementId);
            string downloadFilename = sanitizeFilename(filename + "." + imageType);
            return downloadFilename;
        }

    }
}

