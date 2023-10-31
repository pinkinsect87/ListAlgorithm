using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class ListCompanyCultureBrief
    {
        public int ListCompanyCultureBriefId { get; set; }
        public string VariableName { get; set; }
        public string VariableValue { get; set; }
        public int ListCompanyId { get; set; }

        public virtual ListCompany ListCompany { get; set; }
    }
}
