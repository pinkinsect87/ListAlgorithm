using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class ListCompany
    {
        public ListCompany()
        {
            ListCompanyCultureBriefs = new HashSet<ListCompanyCultureBrief>();
            ListSurveyRespondents = new HashSet<ListSurveyRespondent>();
            ListSurveyRespondentComments = new HashSet<ListSurveyRespondentComment>();
            ListSurveyRespondentDemographics = new HashSet<ListSurveyRespondentDemographic>();
        }

        public int ListCompanyId { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public int? EngagementId { get; set; }
        public string? SurveyVersionId { get; set; }
        public DateTime? CertificationDateTime { get; set; }
        public bool? IsCertified { get; set; }
        public bool? IsDisqualified { get; set; }
        public int ListSourceFileId { get; set; }
        public int ListRequestId { get; set; }
        public DateTime? SurveyDateTime { get; set; }

        public virtual ListRequest ListRequest { get; set; }
        public virtual ListSourceFile ListSourceFile { get; set; }
        public virtual ICollection<ListCompanyCultureBrief> ListCompanyCultureBriefs { get; set; }
        public virtual ICollection<ListSurveyRespondent> ListSurveyRespondents { get; set; }
        public virtual ICollection<ListSurveyRespondentComment> ListSurveyRespondentComments { get; set; }
        public virtual ICollection<ListSurveyRespondentDemographic> ListSurveyRespondentDemographics { get; set; }
    }
}
