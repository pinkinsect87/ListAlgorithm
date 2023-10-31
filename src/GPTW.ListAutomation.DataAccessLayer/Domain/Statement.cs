using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class Statement
    {
        public int StatementId { get; set; }
        public string StatementName { get; set; }
        public int StmtCoreId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public string? CreatedBy { get; set; }
    }
}
