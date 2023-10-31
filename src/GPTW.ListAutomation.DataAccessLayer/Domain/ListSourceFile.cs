using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class ListSourceFile
    {
        public ListSourceFile()
        {
            ListCompanies = new HashSet<ListCompany>();
        }

        public int ListSourceFileId { get; set; }
        public int ListRequestId { get; set; }
        public string FileType { get; set; }
        public Guid? StorageAccountId { get; set; }
        public DateTime? UploadedDateTime { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
        public string? UploadedBy { get; set; }
        public string? ModifiedBy { get; set; }

        public virtual ListRequest ListRequest { get; set; }
        public virtual ICollection<ListCompany> ListCompanies { get; set; }
    }
}
