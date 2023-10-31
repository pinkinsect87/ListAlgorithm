using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class ListSurveyRespondentComment
    {
        public int ListSurveyRespondentCommentsId { get; set; }
        public int ListCompanyId { get; set; }
        public int RespondentKey { get; set; }
        public string? Question { get; set; }
        public string? Response { get; set; }

        public virtual ListCompany ListCompany { get; set; }
    }
}
