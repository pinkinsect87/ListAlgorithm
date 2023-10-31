namespace GPTW.ListAutomation.TestUI.Infrastructure
{
    public static class CultureSurveyConstants
    {
        public static readonly string[] WorksheetsForImport = new string[] {
            "org" , "demographics", "comments"
        };

        public static readonly List<KeyValuePair<string, string>> DemographicsExportColumns =
            new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("Country/Region", "CountryRegion"),
                new KeyValuePair<string, string>("Age", "Age"),
                new KeyValuePair<string, string>("Gender", "Gender"),
                new KeyValuePair<string, string>("Job Level", "JobLevel"),
                new KeyValuePair<string, string>("LGBT/LGBTQ+", "LgbtOrLgbtQ"),
                new KeyValuePair<string, string>("Race/ Ethnicity", "RaceEthniticity"),
                new KeyValuePair<string, string>("Responsibility", "Responsibility"),
                new KeyValuePair<string, string>("Tenure", "Tenure"),
                new KeyValuePair<string, string>("Work Status", "WorkStatus"),
                new KeyValuePair<string, string>("Work Type", "WorkType"),
                new KeyValuePair<string, string>("Worker Type", "WorkerType"),
                new KeyValuePair<string, string>("Birth Year", "BirthYear"),
                new KeyValuePair<string, string>("Confidence", "Confidence"),
                new KeyValuePair<string, string>("Disabilities", "Disabilities"),
                new KeyValuePair<string, string>("Managerial Level", "ManagerialLevel"),
                new KeyValuePair<string, string>("Meaningful Innovation Opportunities", "MeaningfulInnovationOpportunities"),
                new KeyValuePair<string, string>("Pay Type", "PayType"),
                new KeyValuePair<string, string>("Zip Code", "Zipcode")
            };

        public static readonly List<KeyValuePair<string, string>> DemographicsColumns =
            new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("Country/Region", "CountryRegion"),
                new KeyValuePair<string, string>("Age", "Age"),
                new KeyValuePair<string, string>("Gender", "Gender"),
                new KeyValuePair<string, string>("Job Level", "JobLevel"),
                new KeyValuePair<string, string>("JobLevel", "JobLevel"),
                new KeyValuePair<string, string>("LGBT/LGBTQ+", "LgbtOrLgbtQ"),
                new KeyValuePair<string, string>("LGBT", "LgbtOrLgbtQ"),
                new KeyValuePair<string, string>("Race/ Ethnicity", "RaceEthniticity"),
                new KeyValuePair<string, string>("Race/Ethnicity", "RaceEthniticity"),
                new KeyValuePair<string, string>("Responsibility", "Responsibility"),
                new KeyValuePair<string, string>("Tenure", "Tenure"),
                new KeyValuePair<string, string>("Work Status", "WorkStatus"),
                new KeyValuePair<string, string>("WorkStatus", "WorkStatus"),
                new KeyValuePair<string, string>("Work Type", "WorkType"),
                new KeyValuePair<string, string>("WorkType", "WorkType"),
                new KeyValuePair<string, string>("Worker Type", "WorkerType"),
                new KeyValuePair<string, string>("WorkerType", "WorkerType"),
                new KeyValuePair<string, string>("Birth Year", "BirthYear"),
                new KeyValuePair<string, string>("BirthYear", "BirthYear"),
                new KeyValuePair<string, string>("Confidence", "Confidence"),
                new KeyValuePair<string, string>("Disabilities", "Disabilities"),
                new KeyValuePair<string, string>("Managerial Level", "ManagerialLevel"),
                new KeyValuePair<string, string>("ManagerialLevel", "ManagerialLevel"),
                new KeyValuePair<string, string>("Meaningful Innovation Opportunities", "MeaningfulInnovationOpportunities"),
                new KeyValuePair<string, string>("MeaningfulInnovationOpportunities", "MeaningfulInnovationOpportunities"),
                new KeyValuePair<string, string>("Pay Type", "PayType"),
                new KeyValuePair<string, string>("PayType", "PayType"),
                new KeyValuePair<string, string>("Zip Code", "Zipcode"),
                new KeyValuePair<string, string>("ZipCode", "Zipcode")
            };

        public static readonly string[] AgeValues =
           new string[] {
                "No Response", "Under 25", "25 years or younger", "26 years to 34 years", "35 years to 44 years", "45 years to 54 years", "55 years or older"
           };

        public static readonly string[] GenderValues =
           new string[] {
                "No Response", "Female", "Male", "Another gender not listed"
           };

        public static readonly string[] LgbtValues =
           new string[] {
                "No Response", "Prefer not to answer", "Yes", "No"
           };

        public static readonly string[] RaceEthnicityValues =
           new string[] {
                "No Response", "Two or More Races", "Native Hawaiian or Other Pacific Islander", "Hispanic/Latino",
                "Caucasian or White", "Asian", "American Indian or Alaska Native", "African American or Black"
           };

        public static readonly string[] ResponsibilityValues =
           new string[] {
                "No Response", "Children", "Elders", "Both children and elders", "Neither children nor elders"
           };

        public static readonly string[] TenureValues =
           new string[] {
                "No Response", "Less than 2 years", "2 years to 5 years", "6 years to 10 years",
                "11 years to 15 years", "16 years to 20 years", "Over 20 years"
           };

        public static readonly string[] WorkStatusValues =
           new string[] {
                "No Response", "Full-time", "Part-time"
           };

        public static readonly string[] BirthYearValues =
           new string[] {
                "No Response", "1945 or earlier", "Between 1946 and 1964", "Between 1965 and 1980", "Between 1981 and 1997", "1998 or later"
           };

        public static readonly string[] ConfidenceValues =
           new string[] {
                "No Response", "Fair amount", "Great deal", "Just some", "Very little or none"
           };

        public static readonly string[] DisabilitiesValues =
           new string[] {
                "No Response", "No, I do not have a disability", "Prefer not to answer", "Yes, I have a disability"
           };

        public static readonly string[] ManagerialLevelValues =
           new string[] {
                "No Response",
                "Mid-Level Manager (runs major departments or divisions, but not part of executive team)",
                "Employee/Individual Contributor (no people management responsibility)",
                "Executive/C-Level Leader (Highest level leaders; CEO/President and the C-suite executives who report to CEO)",
                "Frontline Manager or Supervisor (first tier manager; supervises other employees, not other managers)"
           };

        public static readonly string[] MeaningfulInnovationOpportunitiesValues =
           new string[] {
                "No Response", "None", "Some", "A lot", "Just a few"
           };

        public static readonly string[] PayTypeValues =
           new string[] {
                "No Response", "Salaried", "Hourly", "Commission"
           };

        public static readonly string[] ZipCodeValues =
           new string[] {
                "No Response",
                "77071", "75231","11111","77142","77405","77571","32257","30605",
                "34236","77103","75050","77041","78745","77003","78741",
                "30311","77354","37163","34209","30312", "77024",
                "77084","78748","75093","28262","37167","78750","77314",
                "77014","77079","78754","77042","75097","75051","77046","77407",
                "34232","77386","77578","30092","30313","30317","30609"
           };

        public static readonly int[] RespondentColumns =
            new int[] {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
                22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
                41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57,
                672, 12211, 12212, 12213, 12214, 12215 };

        #region Comments

        public static readonly string[] CommentsColumns =
           new string[] {
                "question",  "response"
           };

        public static readonly string[] Questions =
           new string[] {
                "Is there anything unique or unusual about this company that makes it a great place to work? Please give specific examples.",
                "If you could change one thing about this company to make it a better place to work, what would it be?",
                "Do you identify as LGBTQ+? (Lesbian, Gay, Bisexual, Transgender, Queer, +) (self-identification)"
           };

        public static readonly string[][] QuestionAnswers =
           new string[][] {
               new string[] {
                   "If I can describe Hilltop in one word it would be, Family. Hilltop does not just treat you like a number. They truly care about their employees and it shows.",
                   "I love how much the management company is involved and how much they care about their employees. It is a nice refreshing change. I cannot wait to make a career with Hilltop Residential.",
                   "I love that everyone knows you by name."
               },
               new string[] {
                   "Everything is great so far. I know it's more exciting things to come.",
                   "I wish they would pay me little bit more money.",
                   "I wish insurance was all paid for."
               },
               new string[] {
                   "Heterosexual",
                   "Transgender",
                   "Bisexual"
               }
           };

        #endregion

    }
}
