using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class ListSurveyRespondentMetadata
    {
        public int ListCompanyMetadataId { get; set; }
        public int ListCompanyResponseId { get; set; }
        public string MetadataKey { get; set; }
        public string? MetadataValue { get; set; }
    }
}
