using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class TemplateType
    {
        public TemplateType()
        {
            ListAlgorithmTemplates = new HashSet<ListAlgorithmTemplate>();
        }

        public int TemplateTypeId { get; set; }
        public string? TemplateTypeName { get; set; }
        public string? TemplateTypeDescription { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        public virtual ICollection<ListAlgorithmTemplate> ListAlgorithmTemplates { get; set; }
    }
}
