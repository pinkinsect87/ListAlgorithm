using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class ListRequest
    {
        public ListRequest()
        {
            ListCompanies = new HashSet<ListCompany>();
            ListSourceFiles = new HashSet<ListSourceFile>();
        }

        public int ListRequestId { get; set; }
        public string CountryCode { get; set; }
        public int TemplateId { get; set; }
        public int PublicationYear { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime ModifiedDateTime { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public int UploadStatusId { get; set; }
        public int AlgorithProcessedStatusId { get; set; }
        public string? ListName { get; set; }
        public string? ListNameLocalLanguage { get; set; }
        public int? ListTypeId { get; set; }
        public string? AffiliateId { get; set; }
        public string? LicenseId { get; set; }
        public int NumberOfWinners { get; set; }
        public int SegmentId { get; set; }

        public virtual Affiliate? Affiliate { get; set; }
        public virtual Status AlgorithProcessedStatus { get; set; }
        public virtual ListAlgorithmTemplate Template { get; set; }
        public virtual Status UploadStatus { get; set; }
        public virtual Segment Segment { get; set; }
        public virtual ICollection<ListCompany> ListCompanies { get; set; }
        public virtual ICollection<ListSourceFile> ListSourceFiles { get; set; }
    }
}
